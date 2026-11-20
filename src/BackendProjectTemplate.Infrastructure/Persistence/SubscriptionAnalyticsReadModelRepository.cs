using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class SubscriptionAnalyticsReadModelRepository(AppReadDbContext dbContext)
    : ISubscriptionAnalyticsReadModelRepository
{
    public async Task<SubscriptionAnalyticsSummaryReadModel> GetSummaryAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var subscriptions = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter);
        var active = ActiveAt(subscriptions, filter.SnapshotUtc);

        var activeCount = await active.LongCountAsync(cancellationToken);
        var newCount = await CountNewAsync(subscriptions, filter.FromUtc, filter.ToExclusiveUtc, cancellationToken);
        var churnedCount = await CountChurnedAsync(subscriptions, filter.FromUtc, filter.ToExclusiveUtc, cancellationToken);
        var cancelAtPeriodEndCount = await active.LongCountAsync(
            item => item.CancelAtPeriodEnd && item.CancellationRequestedAtUtc <= filter.SnapshotUtc,
            cancellationToken);
        var pastDueCount = await active.LongCountAsync(
            item => item.CurrentPeriodEndUtc <= filter.SnapshotUtc &&
                dbContext.SubscriptionRenewals.Any(renewal =>
                    renewal.SubscriptionId == item.Id &&
                    renewal.TargetPeriodStartUtc == item.CurrentPeriodEndUtc &&
                    renewal.Status != SubscriptionRenewalStatus.Fulfilled &&
                    renewal.Status != SubscriptionRenewalStatus.Cancelled &&
                    renewal.Status != SubscriptionRenewalStatus.FulfillmentBlocked),
            cancellationToken);

        var renewals = await GetRenewalsAsync(filter, cancellationToken);
        var revenues = await GetRevenueAtAsync(active, filter.SnapshotUtc, filter.CurrencyId, cancellationToken);
        return new SubscriptionAnalyticsSummaryReadModel(
            activeCount,
            newCount,
            churnedCount,
            pastDueCount,
            cancelAtPeriodEndCount,
            renewals.SuccessfulRenewals,
            renewals.FailedRenewals,
            revenues);
    }

    public async Task<IReadOnlyList<SubscriptionAnalyticsTimelineRow>> GetTimelineAsync(
        SubscriptionAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var subscriptions = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter);
        var subscriptionRows = await subscriptions.Select(item => new SubscriptionTimelineSource
            {
                Id = item.Id,
                BillingAccountId = item.BillingAccountId,
                EntitlementStartUtc = item.EntitlementStartUtc,
                EntitlementEndUtc = item.EntitlementEndUtc,
                EndedAtUtc = item.EndedAtUtc,
                ActivatedAtUtc = dbContext.SubscriptionPeriods
                    .Where(period => period.SubscriptionId == item.Id)
                    .Min(period => (DateTimeOffset?)period.CreatedAtUtc)
            })
            .ToListAsync(cancellationToken);

        var subscriptionIds = subscriptions.Select(item => item.Id);
        var terminalRenewals = await dbContext.SubscriptionRenewals.AsNoTracking()
            .Where(item => subscriptionIds.Contains(item.SubscriptionId) &&
                item.CompletedAtUtc >= filter.FromUtc && item.CompletedAtUtc < filter.ToExclusiveUtc &&
                (item.Status == SubscriptionRenewalStatus.Fulfilled || item.Status == SubscriptionRenewalStatus.Exhausted))
            .Select(item => new RenewalTimelineSource
            {
                CompletedAtUtc = item.CompletedAtUtc!.Value,
                Successful = item.Status == SubscriptionRenewalStatus.Fulfilled
            })
            .ToListAsync(cancellationToken);

        var paidPeriods = await dbContext.SubscriptionPeriods.AsNoTracking()
            .Where(period => subscriptionIds.Contains(period.SubscriptionId) &&
                period.CreatedAtUtc < filter.ToExclusiveUtc &&
                period.ActivationSource != SubscriptionActivationSource.AdministrativeAllocation &&
                (!filter.CurrencyId.HasValue || period.CurrencyId == filter.CurrencyId.Value))
            .Join(
                dbContext.Currencies.AsNoTracking(),
                period => period.CurrencyId,
                currency => currency.Id,
                (period, currency) => new PaidPeriodTimelineSource
                {
                    SubscriptionId = period.SubscriptionId,
                    CreatedAtUtc = period.CreatedAtUtc,
                    Price = period.Price,
                    BillingInterval = period.BillingInterval,
                    CurrencyId = period.CurrencyId,
                    CurrencyCode = currency.CurrencyCode
                })
            .ToListAsync(cancellationToken);

        var from = DateOnly.FromDateTime(filter.FromUtc.UtcDateTime);
        var to = DateOnly.FromDateTime(filter.ToExclusiveUtc.AddDays(-1).UtcDateTime);
        var rows = new List<SubscriptionAnalyticsTimelineRow>();
        for (var periodStart = AnalyticsCalendar.AlignPeriodStart(from, interval);
             periodStart <= to;
             periodStart = AnalyticsCalendar.AdvancePeriod(periodStart, interval))
        {
            var periodEnd = AnalyticsCalendar.GetPeriodEnd(periodStart, to, interval);
            var bucketStartUtc = new DateTimeOffset(periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var bucketEndExclusiveUtc = new DateTimeOffset(periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var snapshotUtc = bucketEndExclusiveUtc.AddTicks(-1);
            var activeRows = subscriptionRows.Where(item => IsActiveAt(item, snapshotUtc)).ToArray();
            var activeIds = activeRows.Select(item => item.Id).ToHashSet();
            var newSubscriptions = subscriptionRows.LongCount(item =>
                item.ActivatedAtUtc >= bucketStartUtc && item.ActivatedAtUtc < bucketEndExclusiveUtc);
            var churnedSubscriptions = subscriptionRows.LongCount(item =>
                item.EndedAtUtc >= bucketStartUtc && item.EndedAtUtc < bucketEndExclusiveUtc &&
                !subscriptionRows.Any(replacement =>
                    replacement.Id != item.Id &&
                    replacement.BillingAccountId == item.BillingAccountId &&
                    IsActiveAt(replacement, item.EndedAtUtc!.Value)));
            var bucketRenewals = terminalRenewals.Where(item =>
                item.CompletedAtUtc >= bucketStartUtc && item.CompletedAtUtc < bucketEndExclusiveUtc).ToArray();
            var revenues = paidPeriods
                .Where(period => activeIds.Contains(period.SubscriptionId) && period.CreatedAtUtc <= snapshotUtc)
                .GroupBy(period => new { period.SubscriptionId, period.CurrencyId, period.CurrencyCode })
                .Select(group => group.OrderByDescending(item => item.CreatedAtUtc).First())
                .GroupBy(period => new { period.CurrencyId, period.CurrencyCode })
                .OrderBy(group => group.Key.CurrencyCode)
                .Select(group => new SubscriptionAnalyticsRevenueRow(
                    group.Key.CurrencyId,
                    group.Key.CurrencyCode,
                    group.Sum(item => SubscriptionRevenueMath.NormalizeMrr(item.Price, item.BillingInterval))))
                .ToArray();

            rows.Add(new SubscriptionAnalyticsTimelineRow(
                periodStart,
                activeRows.LongLength,
                newSubscriptions,
                churnedSubscriptions,
                bucketRenewals.LongCount(item => item.Successful),
                bucketRenewals.LongCount(item => !item.Successful),
                revenues));
        }

        return rows;
    }

    public async Task<SubscriptionAnalyticsRenewalsReadModel> GetRenewalsAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var subscriptionIds = ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter).Select(item => item.Id);
        var renewals = dbContext.SubscriptionRenewals.AsNoTracking().Where(item => subscriptionIds.Contains(item.SubscriptionId));
        var terminal = await renewals
            .Where(item => item.CompletedAtUtc >= filter.FromUtc && item.CompletedAtUtc < filter.ToExclusiveUtc)
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
        await ActiveAt(ApplyFilters(dbContext.Subscriptions.AsNoTracking(), filter), filter.SnapshotUtc)
            .Join(
                dbContext.SubscriptionPlans.AsNoTracking(),
                subscription => subscription.PlanId,
                plan => plan.Id,
                (subscription, plan) => new { Subscription = subscription, Plan = plan })
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

    private IQueryable<Subscription> ActiveAt(IQueryable<Subscription> subscriptions, DateTimeOffset snapshotUtc) =>
        subscriptions.Where(item =>
            item.EntitlementStartUtc <= snapshotUtc &&
            (!item.EntitlementEndUtc.HasValue || item.EntitlementEndUtc > snapshotUtc) &&
            dbContext.SubscriptionPeriods.Any(period =>
                period.SubscriptionId == item.Id && period.CreatedAtUtc <= snapshotUtc));

    private async Task<IReadOnlyList<SubscriptionAnalyticsRevenueRow>> GetRevenueAtAsync(
        IQueryable<Subscription> activeSubscriptions,
        DateTimeOffset snapshotUtc,
        Guid? currencyId,
        CancellationToken cancellationToken) =>
        await (
                from period in dbContext.SubscriptionPeriods.AsNoTracking()
                join subscription in activeSubscriptions on period.SubscriptionId equals subscription.Id
                join currency in dbContext.Currencies.AsNoTracking() on period.CurrencyId equals currency.Id
                where period.CreatedAtUtc <= snapshotUtc &&
                    period.ActivationSource != SubscriptionActivationSource.AdministrativeAllocation &&
                    (!currencyId.HasValue || period.CurrencyId == currencyId.Value) &&
                    period.Id == dbContext.SubscriptionPeriods
                        .Where(candidate => candidate.SubscriptionId == period.SubscriptionId &&
                            candidate.CreatedAtUtc <= snapshotUtc &&
                            candidate.ActivationSource != SubscriptionActivationSource.AdministrativeAllocation)
                        .OrderByDescending(candidate => candidate.CreatedAtUtc)
                        .ThenByDescending(candidate => candidate.Id)
                        .Select(candidate => candidate.Id)
                        .First()
                group period by new { period.CurrencyId, currency.CurrencyCode }
                into groupItems
                orderby groupItems.Key.CurrencyCode
                select new SubscriptionAnalyticsRevenueRow(
                    groupItems.Key.CurrencyId,
                    groupItems.Key.CurrencyCode,
                    groupItems.Sum(item => item.BillingInterval == BillingInterval.Monthly
                        ? item.Price
                        : item.Price / 12m)))
            .ToListAsync(cancellationToken);

    private async Task<long> CountNewAsync(
        IQueryable<Subscription> subscriptions,
        DateTimeOffset fromUtc,
        DateTimeOffset toExclusiveUtc,
        CancellationToken cancellationToken) =>
        await subscriptions.LongCountAsync(subscription =>
            dbContext.SubscriptionPeriods
                .Where(period => period.SubscriptionId == subscription.Id)
                .Min(period => (DateTimeOffset?)period.CreatedAtUtc) >= fromUtc &&
            dbContext.SubscriptionPeriods
                .Where(period => period.SubscriptionId == subscription.Id)
                .Min(period => (DateTimeOffset?)period.CreatedAtUtc) < toExclusiveUtc,
            cancellationToken);

    private async Task<long> CountChurnedAsync(
        IQueryable<Subscription> subscriptions,
        DateTimeOffset fromUtc,
        DateTimeOffset toExclusiveUtc,
        CancellationToken cancellationToken) =>
        await subscriptions.LongCountAsync(item =>
            item.EndedAtUtc >= fromUtc && item.EndedAtUtc < toExclusiveUtc &&
            !dbContext.Subscriptions.Any(replacement =>
                replacement.Id != item.Id &&
                replacement.TenantId == item.TenantId &&
                replacement.BillingAccountId == item.BillingAccountId &&
                replacement.EntitlementStartUtc <= item.EndedAtUtc &&
                (!replacement.EntitlementEndUtc.HasValue || replacement.EntitlementEndUtc > item.EndedAtUtc) &&
                dbContext.SubscriptionPeriods.Any(period =>
                    period.SubscriptionId == replacement.Id && period.CreatedAtUtc <= item.EndedAtUtc)),
            cancellationToken);

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
            subscriptions = subscriptions.Where(item => dbContext.SubscriptionPeriods.Any(period =>
                period.SubscriptionId == item.Id && period.CurrencyId == filter.CurrencyId.Value));
        return subscriptions;
    }

    private static bool IsActiveAt(SubscriptionTimelineSource item, DateTimeOffset snapshotUtc) =>
        item.ActivatedAtUtc.HasValue &&
        item.ActivatedAtUtc.Value <= snapshotUtc &&
        item.EntitlementStartUtc <= snapshotUtc &&
        (!item.EntitlementEndUtc.HasValue || item.EntitlementEndUtc > snapshotUtc);

    private sealed class SubscriptionTimelineSource
    {
        public Guid Id { get; init; }
        public Guid BillingAccountId { get; init; }
        public DateTimeOffset? EntitlementStartUtc { get; init; }
        public DateTimeOffset? EntitlementEndUtc { get; init; }
        public DateTimeOffset? EndedAtUtc { get; init; }
        public DateTimeOffset? ActivatedAtUtc { get; init; }
    }

    private sealed class RenewalTimelineSource
    {
        public DateTimeOffset CompletedAtUtc { get; init; }
        public bool Successful { get; init; }
    }

    private sealed class PaidPeriodTimelineSource
    {
        public Guid SubscriptionId { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public decimal Price { get; init; }
        public BillingInterval BillingInterval { get; init; }
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
    }
}
