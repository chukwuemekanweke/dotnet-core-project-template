using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Reconstructs subscription analytics at historical instants from durable records only: the immutable
/// <see cref="SubscriptionPeriod"/> snapshots (and when they were persisted), the entitlement window, which is
/// only ever closed at the moment of cancellation or expiry, and <see cref="Subscription.EndedAtUtc"/>. Current
/// status, <see cref="Subscription.CurrentPeriodEndUtc"/> and renewal status never decide a historical state.
/// Summary, timeline and plan breakdown share one activity projection so their numbers reconcile, and every
/// timeline series is aggregated in PostgreSQL per UTC day (bounded by the reporting range) before being rolled
/// into calendar buckets.
/// </summary>
public sealed class SubscriptionAnalyticsReadModelRepository(AppReadDbContext dbContext)
    : ISubscriptionAnalyticsReadModelRepository
{
    public async Task<SubscriptionAnalyticsSummaryReadModel> GetSummaryAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var boundaryUtc = filter.ToExclusiveUtc;
        var activities = Activities(filter);
        var active = ActiveAt(activities, boundaryUtc);

        var activeCount = await active.LongCountAsync(cancellationToken);
        var newCount = await activities.LongCountAsync(
            item => item.ActivatedAtUtc >= filter.FromUtc && item.ActivatedAtUtc < boundaryUtc,
            cancellationToken);
        var churnedCount = await Churned(activities, filter).LongCountAsync(cancellationToken);
        var cancelAtPeriodEndCount = await active.LongCountAsync(
            item => item.CancelAtPeriodEnd && item.CancellationRequestedAtUtc < boundaryUtc,
            cancellationToken);
        var pastDueCount = await active.LongCountAsync(
            item => dbContext.SubscriptionPeriods
                .Where(period => period.SubscriptionId == item.Id && period.CreatedAtUtc < boundaryUtc)
                .Max(period => (DateTimeOffset?)period.PeriodEndUtc) < boundaryUtc,
            cancellationToken);
        var renewals = await TerminalRenewals(filter)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Successful = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Fulfilled),
                Failed = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Exhausted)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var revenues = await GetRevenueAtAsync(filter, boundaryUtc, cancellationToken);

        return new SubscriptionAnalyticsSummaryReadModel(
            activeCount,
            newCount,
            churnedCount,
            pastDueCount,
            cancelAtPeriodEndCount,
            renewals?.Successful ?? 0,
            renewals?.Failed ?? 0,
            ToRevenueRows(revenues));
    }

    public async Task<IReadOnlyList<SubscriptionAnalyticsTimelineRow>> GetTimelineAsync(
        SubscriptionAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var fromUtc = filter.FromUtc;
        var toExclusiveUtc = filter.ToExclusiveUtc;
        var activities = Activities(filter);

        var activeBefore = await ActiveAt(activities, fromUtc).LongCountAsync(cancellationToken);
        var activations = await CountByDayAsync(
            activities
                .Where(item => item.ActivatedAtUtc >= fromUtc && item.ActivatedAtUtc < toExclusiveUtc)
                .GroupBy(item => item.ActivatedAtUtc.Date),
            cancellationToken);
        var endings = await CountByDayAsync(
            activities
                .Where(item => item.EndsAtUtc >= fromUtc && item.EndsAtUtc < toExclusiveUtc)
                .GroupBy(item => item.EndsAtUtc!.Value.Date),
            cancellationToken);
        var churn = await CountByDayAsync(
            Churned(activities, filter).GroupBy(item => item.EndsAtUtc!.Value.Date),
            cancellationToken);
        var renewals = (await TerminalRenewals(filter)
                .GroupBy(item => item.CompletedAtUtc!.Value.Date)
                .Select(group => new
                {
                    Day = group.Key,
                    Successful = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Fulfilled),
                    Failed = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Exhausted)
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(item => DateOnly.FromDateTime(item.Day), item => (item.Successful, item.Failed));

        var revenueIntervals = RevenueIntervals(filter);
        var revenueBefore = await SumRevenueAsync(
            revenueIntervals.Where(item =>
                item.StartsAtUtc < fromUtc && (item.EndsAtUtc == null || item.EndsAtUtc >= fromUtc)),
            cancellationToken);
        var revenueStarts = await SumRevenueByDayAsync(
            revenueIntervals
                .Where(item => item.StartsAtUtc >= fromUtc && item.StartsAtUtc < toExclusiveUtc)
                .Select(item => new RevenueTotal
                {
                    Day = item.StartsAtUtc.Date,
                    CurrencyId = item.CurrencyId,
                    CurrencyCode = item.CurrencyCode,
                    BillingInterval = item.BillingInterval,
                    Total = item.Price
                }),
            cancellationToken);
        var revenueEnds = await SumRevenueByDayAsync(
            revenueIntervals
                .Where(item => item.EndsAtUtc >= fromUtc && item.EndsAtUtc < toExclusiveUtc)
                .Select(item => new RevenueTotal
                {
                    Day = item.EndsAtUtc!.Value.Date,
                    CurrencyId = item.CurrencyId,
                    CurrencyCode = item.CurrencyCode,
                    BillingInterval = item.BillingInterval,
                    Total = item.Price
                }),
            cancellationToken);

        var runningRevenue = new Dictionary<RevenueKey, decimal>();
        foreach (var item in revenueBefore)
            Accumulate(runningRevenue, item, 1m);

        var currencies = revenueBefore.Concat(revenueStarts).Concat(revenueEnds)
            .Select(item => (item.CurrencyId, item.CurrencyCode))
            .Distinct()
            .OrderBy(item => item.CurrencyCode, StringComparer.Ordinal)
            .ToArray();
        var revenueStartsByDay = revenueStarts.ToLookup(item => DateOnly.FromDateTime(item.Day));
        var revenueEndsByDay = revenueEnds.ToLookup(item => DateOnly.FromDateTime(item.Day));

        var from = DateOnly.FromDateTime(fromUtc.UtcDateTime);
        var to = DateOnly.FromDateTime(toExclusiveUtc.UtcDateTime).AddDays(-1);
        var runningActive = activeBefore;
        var rows = new List<SubscriptionAnalyticsTimelineRow>();
        for (var periodStart = AnalyticsCalendar.AlignPeriodStart(from, interval);
             periodStart <= to;
             periodStart = AnalyticsCalendar.AdvancePeriod(periodStart, interval))
        {
            var periodEnd = AnalyticsCalendar.GetPeriodEnd(periodStart, to, interval);
            long newSubscriptions = 0, churnedSubscriptions = 0, successfulRenewals = 0, failedRenewals = 0;
            for (var day = periodStart < from ? from : periodStart; day <= periodEnd; day = day.AddDays(1))
            {
                var started = activations.GetValueOrDefault(day);
                runningActive += started - endings.GetValueOrDefault(day);
                newSubscriptions += started;
                churnedSubscriptions += churn.GetValueOrDefault(day);
                var dayRenewals = renewals.GetValueOrDefault(day);
                successfulRenewals += dayRenewals.Successful;
                failedRenewals += dayRenewals.Failed;
                foreach (var item in revenueStartsByDay[day])
                    Accumulate(runningRevenue, item, 1m);
                foreach (var item in revenueEndsByDay[day])
                    Accumulate(runningRevenue, item, -1m);
            }

            rows.Add(new SubscriptionAnalyticsTimelineRow(
                periodStart,
                runningActive,
                newSubscriptions,
                churnedSubscriptions,
                successfulRenewals,
                failedRenewals,
                currencies.Select(currency => new SubscriptionAnalyticsRevenueRow(
                        currency.CurrencyId,
                        currency.CurrencyCode,
                        runningRevenue
                            .Where(item => item.Key.CurrencyId == currency.CurrencyId)
                            .Sum(item => SubscriptionRevenueMath.NormalizeMrr(item.Value, item.Key.BillingInterval))))
                    .ToArray()));
        }

        return rows;
    }

    public async Task<SubscriptionAnalyticsRenewalsReadModel> GetRenewalsAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var subscriptionIds = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter).Select(item => item.Id);
        var renewals = dbContext.SubscriptionRenewals.AsNoTracking().Where(item => subscriptionIds.Contains(item.SubscriptionId));
        var terminal = await TerminalRenewals(filter)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Successful = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Fulfilled),
                Failed = group.LongCount(item => item.Status == SubscriptionRenewalStatus.Exhausted)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var pending = await renewals.LongCountAsync(item =>
            item.Status == SubscriptionRenewalStatus.Pending ||
            item.Status == SubscriptionRenewalStatus.AwaitingAuthorization ||
            item.Status == SubscriptionRenewalStatus.Charging ||
            item.Status == SubscriptionRenewalStatus.RetryScheduled,
            cancellationToken);
        var awaitingReconciliation = await renewals.LongCountAsync(
            item => item.Status == SubscriptionRenewalStatus.AwaitingReconciliation,
            cancellationToken);
        var failedAttempts = await dbContext.SubscriptionRenewalAttempts.AsNoTracking().LongCountAsync(
            attempt => renewals.Any(renewal => renewal.Id == attempt.RenewalId) &&
                attempt.Status == SubscriptionRenewalAttemptStatus.Failed &&
                attempt.CompletedAtUtc >= filter.FromUtc && attempt.CompletedAtUtc < filter.ToExclusiveUtc,
            cancellationToken);

        return new SubscriptionAnalyticsRenewalsReadModel(
            terminal?.Successful ?? 0,
            terminal?.Failed ?? 0,
            failedAttempts,
            pending,
            awaitingReconciliation);
    }

    public async Task<IReadOnlyList<SubscriptionAnalyticsPlanRow>> GetPlansAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken) =>
        await ActiveAt(Activities(filter), filter.ToExclusiveUtc)
            .Join(
                dbContext.SubscriptionPlans.AsNoTracking(),
                activity => activity.PlanId,
                plan => plan.Id,
                (activity, plan) => new { activity.Id, Plan = plan })
            .GroupBy(item => new { item.Plan.Id, item.Plan.Code, item.Plan.Name })
            .OrderByDescending(group => group.LongCount())
            .ThenBy(group => group.Key.Code)
            .Select(group => new SubscriptionAnalyticsPlanRow(
                group.Key.Id,
                group.Key.Code,
                group.Key.Name,
                group.LongCount()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubscriptionAnalyticsStatusRow>> GetStatusesAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var subscriptionIds = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter).Select(item => item.Id);
        return await dbContext.SubscriptionStatusHistory.AsNoTracking()
            .Where(history => subscriptionIds.Contains(history.SubscriptionId) &&
                history.OccurredAtUtc <= filter.SnapshotUtc &&
                history.Id == dbContext.SubscriptionStatusHistory
                    .Where(candidate => candidate.SubscriptionId == history.SubscriptionId &&
                        candidate.OccurredAtUtc <= filter.SnapshotUtc)
                    .OrderByDescending(candidate => candidate.OccurredAtUtc)
                    .ThenByDescending(candidate => candidate.Id)
                    .Select(candidate => candidate.Id)
                    .First())
            .GroupBy(history => history.NewStatus)
            .OrderBy(group => group.Key)
            .Select(group => new SubscriptionAnalyticsStatusRow(group.Key, group.LongCount()))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// One row per filtered subscription that actually held entitlement. Activation is the first instant the
    /// subscription was both inside its entitlement window and backed by a persisted period, so pending
    /// purchases, unpaid expiries and lapsed or pre-start-cancelled allocations never appear. The effective end
    /// falls back to <see cref="Subscription.EndedAtUtc"/> because expiry of an auto-renewing subscription
    /// (for example after exhausted renewals) leaves its open-ended entitlement window unclosed.
    /// </summary>
    private IQueryable<SubscriptionActivity> Activities(SubscriptionAnalyticsFilter filter) =>
        (from subscription in ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter)
         from firstPeriod in dbContext.SubscriptionPeriods.AsNoTracking()
             .Where(period => period.SubscriptionId == subscription.Id)
             .OrderBy(period => period.CreatedAtUtc)
             .Take(1)
         select new SubscriptionActivity
         {
             Id = subscription.Id,
             TenantId = subscription.TenantId,
             BillingAccountId = subscription.BillingAccountId,
             PlanId = subscription.PlanId,
             CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
             CancellationRequestedAtUtc = subscription.CancellationRequestedAtUtc,
             EndedAtUtc = subscription.EndedAtUtc,
             ActivatedAtUtc = subscription.EntitlementStartUtc > firstPeriod.CreatedAtUtc
                 ? subscription.EntitlementStartUtc.Value
                 : firstPeriod.CreatedAtUtc,
             EndsAtUtc = subscription.EntitlementEndUtc ?? subscription.EndedAtUtc
         })
        .Where(item => item.EndsAtUtc == null || item.EndsAtUtc > item.ActivatedAtUtc);

    /// <summary>Entitled at the final instant before <paramref name="boundaryUtc"/>: <c>[activation, end)</c>.</summary>
    private static IQueryable<SubscriptionActivity> ActiveAt(
        IQueryable<SubscriptionActivity> activities,
        DateTimeOffset boundaryUtc) =>
        activities.Where(item =>
            item.ActivatedAtUtc < boundaryUtc &&
            (item.EndsAtUtc == null || item.EndsAtUtc >= boundaryUtc));

    /// <summary>
    /// Subscriptions whose lifecycle ended inside the range, at their effective entitlement end, while no other
    /// subscription of the same BillingAccount held entitlement at that instant. The replacement search ignores
    /// plan, owner-type and currency filters on purpose: a plan or currency switch on the same account is not
    /// customer churn, whichever slice is being reported.
    /// </summary>
    private IQueryable<SubscriptionActivity> Churned(
        IQueryable<SubscriptionActivity> activities,
        SubscriptionAnalyticsFilter filter) =>
        activities.Where(item =>
            item.EndedAtUtc != null &&
            item.EndsAtUtc >= filter.FromUtc && item.EndsAtUtc < filter.ToExclusiveUtc &&
            !dbContext.Subscriptions.Any(replacement =>
                replacement.Id != item.Id &&
                replacement.TenantId == item.TenantId &&
                replacement.BillingAccountId == item.BillingAccountId &&
                replacement.EntitlementStartUtc <= item.EndsAtUtc &&
                ((replacement.EntitlementEndUtc ?? replacement.EndedAtUtc) == null ||
                    (replacement.EntitlementEndUtc ?? replacement.EndedAtUtc) > item.EndsAtUtc) &&
                dbContext.SubscriptionPeriods.Any(period => period.SubscriptionId == replacement.Id)));

    /// <summary>
    /// Distinct renewals that reached a terminal financial outcome inside the range. Failed payment attempts
    /// that a later attempt recovers never count as failed renewals.
    /// </summary>
    private IQueryable<SubscriptionRenewal> TerminalRenewals(SubscriptionAnalyticsFilter filter)
    {
        var subscriptionIds = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter).Select(item => item.Id);
        return dbContext.SubscriptionRenewals.AsNoTracking()
            .Where(item => subscriptionIds.Contains(item.SubscriptionId) &&
                item.CompletedAtUtc >= filter.FromUtc && item.CompletedAtUtc < filter.ToExclusiveUtc &&
                (item.Status == SubscriptionRenewalStatus.Fulfilled || item.Status == SubscriptionRenewalStatus.Exhausted));
    }

    /// <summary>
    /// The time span during which each paid period snapshot is the subscription's effective price: from when it
    /// was persisted (never before activation) until the next paid period of the same subscription was persisted
    /// or entitlement ended. At most one period per subscription is effective at any instant, so duplicate
    /// deliveries and overlapping periods cannot double count. Administrative allocations carry nominal prices
    /// and are excluded.
    /// </summary>
    private IQueryable<RevenueInterval> RevenueIntervals(SubscriptionAnalyticsFilter filter) =>
        (from period in dbContext.SubscriptionPeriods.AsNoTracking()
         join activity in Activities(filter) on period.SubscriptionId equals activity.Id
         join currency in dbContext.Currencies.AsNoTracking() on period.CurrencyId equals currency.Id
         where period.ActivationSource != SubscriptionActivationSource.AdministrativeAllocation &&
             (!filter.CurrencyId.HasValue || period.CurrencyId == filter.CurrencyId.Value)
         from successor in dbContext.SubscriptionPeriods.AsNoTracking()
             .Where(candidate => candidate.SubscriptionId == period.SubscriptionId &&
                 candidate.ActivationSource != SubscriptionActivationSource.AdministrativeAllocation &&
                 (candidate.CreatedAtUtc > period.CreatedAtUtc ||
                     (candidate.CreatedAtUtc == period.CreatedAtUtc && candidate.PeriodStartUtc > period.PeriodStartUtc)))
             .OrderBy(candidate => candidate.CreatedAtUtc)
             .Select(candidate => (DateTimeOffset?)candidate.CreatedAtUtc)
             .Take(1)
             .DefaultIfEmpty()
         select new RevenueInterval
         {
             CurrencyId = period.CurrencyId,
             CurrencyCode = currency.CurrencyCode,
             BillingInterval = period.BillingInterval,
             Price = period.Price,
             StartsAtUtc = period.CreatedAtUtc > activity.ActivatedAtUtc ? period.CreatedAtUtc : activity.ActivatedAtUtc,
             EndsAtUtc = successor == null
                 ? activity.EndsAtUtc
                 : activity.EndsAtUtc == null || successor < activity.EndsAtUtc
                     ? successor
                     : activity.EndsAtUtc
         })
        .Where(item => item.EndsAtUtc == null || item.EndsAtUtc > item.StartsAtUtc);

    private async Task<IReadOnlyList<RevenueTotal>> GetRevenueAtAsync(
        SubscriptionAnalyticsFilter filter,
        DateTimeOffset boundaryUtc,
        CancellationToken cancellationToken) =>
        await SumRevenueAsync(
            RevenueIntervals(filter).Where(item =>
                item.StartsAtUtc < boundaryUtc && (item.EndsAtUtc == null || item.EndsAtUtc >= boundaryUtc)),
            cancellationToken);

    private static async Task<IReadOnlyList<RevenueTotal>> SumRevenueAsync(
        IQueryable<RevenueInterval> intervals,
        CancellationToken cancellationToken) =>
        await intervals
            .GroupBy(item => new { item.CurrencyId, item.CurrencyCode, item.BillingInterval })
            .Select(group => new RevenueTotal
            {
                CurrencyId = group.Key.CurrencyId,
                CurrencyCode = group.Key.CurrencyCode,
                BillingInterval = group.Key.BillingInterval,
                Total = group.Sum(item => item.Price)
            })
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<RevenueTotal>> SumRevenueByDayAsync(
        IQueryable<RevenueTotal> dailyPrices,
        CancellationToken cancellationToken) =>
        await dailyPrices
            .GroupBy(item => new { item.Day, item.CurrencyId, item.CurrencyCode, item.BillingInterval })
            .Select(group => new RevenueTotal
            {
                Day = group.Key.Day,
                CurrencyId = group.Key.CurrencyId,
                CurrencyCode = group.Key.CurrencyCode,
                BillingInterval = group.Key.BillingInterval,
                Total = group.Sum(item => item.Total)
            })
            .ToListAsync(cancellationToken);

    private static async Task<Dictionary<DateOnly, long>> CountByDayAsync(
        IQueryable<IGrouping<DateTime, SubscriptionActivity>> groups,
        CancellationToken cancellationToken) =>
        (await groups.Select(group => new { Day = group.Key, Count = group.LongCount() }).ToListAsync(cancellationToken))
            .ToDictionary(item => DateOnly.FromDateTime(item.Day), item => item.Count);

    private static IReadOnlyList<SubscriptionAnalyticsRevenueRow> ToRevenueRows(IEnumerable<RevenueTotal> totals) =>
        totals
            .GroupBy(item => (item.CurrencyId, item.CurrencyCode))
            .OrderBy(group => group.Key.CurrencyCode, StringComparer.Ordinal)
            .Select(group => new SubscriptionAnalyticsRevenueRow(
                group.Key.CurrencyId,
                group.Key.CurrencyCode,
                group.Sum(item => SubscriptionRevenueMath.NormalizeMrr(item.Total, item.BillingInterval))))
            .ToArray();

    private static void Accumulate(Dictionary<RevenueKey, decimal> running, RevenueTotal item, decimal sign)
    {
        var key = new RevenueKey(item.CurrencyId, item.BillingInterval);
        running[key] = running.GetValueOrDefault(key) + sign * item.Total;
    }

    /// <summary>
    /// Plan, tenant and owner type select subscriptions directly. Currency selects subscriptions denominated in
    /// that currency, which is their plan's immutable currency and therefore also every period's currency.
    /// </summary>
    private IQueryable<Subscription> ApplyFilters(
        IQueryable<Subscription> subscriptions,
        SubscriptionAnalyticsFilter filter)
    {
        if (filter.PlanId.HasValue)
            subscriptions = subscriptions.Where(item => item.PlanId == filter.PlanId.Value);
        if (filter.TenantId.HasValue)
            subscriptions = subscriptions.Where(item => item.TenantId == filter.TenantId.Value);
        if (filter.OwnerType.HasValue)
            subscriptions = subscriptions.Where(item => dbContext.BillingAccounts.Any(account =>
                account.Id == item.BillingAccountId &&
                account.TenantId == item.TenantId &&
                account.OwnerType == filter.OwnerType.Value));
        if (filter.CurrencyId.HasValue)
            subscriptions = subscriptions.Where(item => dbContext.SubscriptionPlans.Any(plan =>
                plan.Id == item.PlanId && plan.CurrencyId == filter.CurrencyId.Value));
        return subscriptions;
    }

    private readonly record struct RevenueKey(Guid CurrencyId, BillingInterval BillingInterval);

    private sealed class SubscriptionActivity
    {
        public Guid Id { get; init; }
        public Guid TenantId { get; init; }
        public Guid BillingAccountId { get; init; }
        public Guid PlanId { get; init; }
        public bool CancelAtPeriodEnd { get; init; }
        public DateTimeOffset? CancellationRequestedAtUtc { get; init; }
        public DateTimeOffset? EndedAtUtc { get; init; }
        public DateTimeOffset ActivatedAtUtc { get; init; }
        public DateTimeOffset? EndsAtUtc { get; init; }
    }

    private sealed class RevenueInterval
    {
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public BillingInterval BillingInterval { get; init; }
        public decimal Price { get; init; }
        public DateTimeOffset StartsAtUtc { get; init; }
        public DateTimeOffset? EndsAtUtc { get; init; }
    }

    private sealed class RevenueTotal
    {
        public DateTime Day { get; init; }
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public BillingInterval BillingInterval { get; init; }
        public decimal Total { get; init; }
    }
}
