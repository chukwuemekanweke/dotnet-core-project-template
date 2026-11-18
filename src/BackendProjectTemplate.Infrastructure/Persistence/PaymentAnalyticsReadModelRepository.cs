using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class PaymentAnalyticsReadModelRepository(AppReadDbContext dbContext)
    : IPaymentAnalyticsReadModelRepository
{
    public async Task<PaymentAnalyticsSummaryReadModel> GetSummaryAsync(
        PaymentAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var payments = ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), filter);
        var totals = await payments.GroupBy(_ => 1)
            .Select(group => new
            {
                TransactionCount = group.LongCount(),
                SuccessfulCount = group.LongCount(item => item.PaymentStatus == PaymentStatus.Succeeded),
                UnsuccessfulCount = group.LongCount(item => item.PaymentStatus == PaymentStatus.Failed ||
                    item.PaymentStatus == PaymentStatus.Cancelled || item.PaymentStatus == PaymentStatus.Expired),
                ProcessingCount = group.LongCount(item => item.PaymentStatus == PaymentStatus.PendingInitiation ||
                    item.PaymentStatus == PaymentStatus.Initiated)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var currencies = await (
                from payment in payments
                join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
                group payment by new { payment.CurrencyId, currency.CurrencyCode }
                into groupItems
                orderby groupItems.Key.CurrencyCode, groupItems.Key.CurrencyId
                select new PaymentAnalyticsCurrencyRow(
                    groupItems.Key.CurrencyId,
                    groupItems.Key.CurrencyCode,
                    groupItems.LongCount(),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Succeeded),
                    groupItems.Sum(item => item.Amount),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Succeeded ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Failed ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Cancelled ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Expired ? item.Amount : 0m)))
            .ToListAsync(cancellationToken);

        return new PaymentAnalyticsSummaryReadModel(
            totals?.TransactionCount ?? 0,
            totals?.SuccessfulCount ?? 0,
            totals?.UnsuccessfulCount ?? 0,
            totals?.ProcessingCount ?? 0,
            currencies);
    }

    public Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineAsync(
        PaymentAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var payments = ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), filter);
        return interval switch
        {
            Interval.Day => GetTimelineByDayAsync(payments, cancellationToken),
            Interval.Week => GetTimelineByWeekAsync(payments, cancellationToken),
            Interval.Month => GetTimelineByMonthAsync(payments, cancellationToken),
            Interval.Year => GetTimelineByYearAsync(payments, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }

    public async Task<IReadOnlyList<PaymentAnalyticsStatusRow>> GetStatusesAsync(
        PaymentAnalyticsFilter filter,
        CancellationToken cancellationToken) =>
        await (
                from payment in ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), filter)
                join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
                group payment by new { payment.PaymentStatus, payment.CurrencyId, currency.CurrencyCode }
                into groupItems
                orderby groupItems.Key.CurrencyCode, groupItems.Key.PaymentStatus
                select new PaymentAnalyticsStatusRow(
                    groupItems.Key.PaymentStatus,
                    groupItems.Key.CurrencyId,
                    groupItems.Key.CurrencyCode,
                    groupItems.LongCount(),
                    groupItems.Sum(item => item.Amount)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAnalyticsProviderRow>> GetProvidersAsync(
        PaymentAnalyticsFilter filter,
        CancellationToken cancellationToken) =>
        await (
                from payment in ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), filter)
                join provider in dbContext.PaymentProviders.AsNoTracking() on payment.PaymentProviderId equals provider.Id
                join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
                group payment by new
                {
                    payment.PaymentProviderId,
                    provider.ProviderName,
                    provider.ProviderKey,
                    payment.CurrencyId,
                    currency.CurrencyCode
                }
                into groupItems
                orderby groupItems.Key.ProviderName, groupItems.Key.CurrencyCode
                select new PaymentAnalyticsProviderRow(
                    groupItems.Key.PaymentProviderId,
                    groupItems.Key.ProviderName,
                    groupItems.Key.ProviderKey,
                    groupItems.Key.CurrencyId,
                    groupItems.Key.CurrencyCode,
                    groupItems.LongCount(),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Succeeded),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Failed || item.PaymentStatus == PaymentStatus.Cancelled || item.PaymentStatus == PaymentStatus.Expired),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated),
                    groupItems.Sum(item => item.Amount),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Succeeded ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Failed ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Cancelled ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Expired ? item.Amount : 0m)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAnalyticsIntentRow>> GetIntentsAsync(
        PaymentAnalyticsFilter filter,
        CancellationToken cancellationToken) =>
        await (
                from payment in ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), filter)
                join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
                group payment by new { payment.PaymentIntent, payment.CurrencyId, currency.CurrencyCode }
                into groupItems
                orderby groupItems.Key.PaymentIntent, groupItems.Key.CurrencyCode
                select new PaymentAnalyticsIntentRow(
                    groupItems.Key.PaymentIntent,
                    groupItems.Key.CurrencyId,
                    groupItems.Key.CurrencyCode,
                    groupItems.LongCount(),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Succeeded),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Failed || item.PaymentStatus == PaymentStatus.Cancelled || item.PaymentStatus == PaymentStatus.Expired),
                    groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated),
                    groupItems.Sum(item => item.Amount),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Succeeded ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Failed ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Cancelled ? item.Amount : 0m),
                    groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Expired ? item.Amount : 0m)))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineByDayAsync(
        IQueryable<PaymentTransaction> payments,
        CancellationToken cancellationToken) =>
        MapTimeline(await GroupTimeline(payments.Select(payment => new TimelineSource<DayPeriod>
            {
                Payment = payment,
                Period = new DayPeriod(payment.CreatedAtUtc.Year, payment.CreatedAtUtc.Month, payment.CreatedAtUtc.Day)
            }))
            .ToListAsync(cancellationToken), key => new DateOnly(key.Year, key.Month, key.Day));

    private async Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineByWeekAsync(
        IQueryable<PaymentTransaction> payments,
        CancellationToken cancellationToken)
    {
        var rows = await GroupTimeline(payments.Select(payment => new TimelineSource<DateTime>
            {
                Payment = payment,
                Period = payment.CreatedAtUtc.Date.AddDays(-(((int)payment.CreatedAtUtc.DayOfWeek + 6) % 7))
            }))
            .ToListAsync(cancellationToken);
        return MapTimeline(rows, DateOnly.FromDateTime);
    }

    private async Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineByMonthAsync(
        IQueryable<PaymentTransaction> payments,
        CancellationToken cancellationToken) =>
        MapTimeline(await GroupTimeline(payments.Select(payment => new TimelineSource<MonthPeriod>
            {
                Payment = payment,
                Period = new MonthPeriod(payment.CreatedAtUtc.Year, payment.CreatedAtUtc.Month)
            }))
            .ToListAsync(cancellationToken), key => new DateOnly(key.Year, key.Month, 1));

    private async Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineByYearAsync(
        IQueryable<PaymentTransaction> payments,
        CancellationToken cancellationToken) =>
        MapTimeline(await GroupTimeline(payments.Select(payment => new TimelineSource<int>
            {
                Payment = payment,
                Period = payment.CreatedAtUtc.Year
            }))
            .ToListAsync(cancellationToken), year => new DateOnly(year, 1, 1));

    private IQueryable<TimelineAggregate<TKey>> GroupTimeline<TKey>(
        IQueryable<TimelineSource<TKey>> source) where TKey : notnull =>
        from item in source
        join currency in dbContext.Currencies.AsNoTracking() on item.Payment.CurrencyId equals currency.Id
        group item.Payment by new { item.Period, item.Payment.CurrencyId, currency.CurrencyCode }
        into groupItems
        select new TimelineAggregate<TKey>
        {
            Period = groupItems.Key.Period,
            CurrencyId = groupItems.Key.CurrencyId,
            CurrencyCode = groupItems.Key.CurrencyCode,
            TransactionCount = groupItems.LongCount(),
            SuccessfulCount = groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Succeeded),
            UnsuccessfulCount = groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.Failed || item.PaymentStatus == PaymentStatus.Cancelled || item.PaymentStatus == PaymentStatus.Expired),
            ProcessingCount = groupItems.LongCount(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated),
            AttemptedVolume = groupItems.Sum(item => item.Amount),
            SuccessfulVolume = groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Succeeded ? item.Amount : 0m),
            FailedVolume = groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Failed ? item.Amount : 0m),
            ProcessingVolume = groupItems.Sum(item => item.PaymentStatus == PaymentStatus.PendingInitiation || item.PaymentStatus == PaymentStatus.Initiated ? item.Amount : 0m),
            CancelledVolume = groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Cancelled ? item.Amount : 0m),
            ExpiredVolume = groupItems.Sum(item => item.PaymentStatus == PaymentStatus.Expired ? item.Amount : 0m)
        };

    private static IReadOnlyList<PaymentAnalyticsTimelineRow> MapTimeline<TKey>(
        IEnumerable<TimelineAggregate<TKey>> rows,
        Func<TKey, DateOnly> dateSelector) where TKey : notnull =>
        rows.Select(row => new PaymentAnalyticsTimelineRow(
                dateSelector(row.Period), row.CurrencyId, row.CurrencyCode, row.TransactionCount,
                row.SuccessfulCount, row.UnsuccessfulCount, row.ProcessingCount, row.AttemptedVolume,
                row.SuccessfulVolume, row.FailedVolume, row.ProcessingVolume, row.CancelledVolume, row.ExpiredVolume))
            .OrderBy(row => row.PeriodStart)
            .ThenBy(row => row.CurrencyCode, StringComparer.Ordinal)
            .ToArray();

    private static IQueryable<PaymentTransaction> ApplyFilters(
        IQueryable<PaymentTransaction> payments,
        PaymentAnalyticsFilter filter)
    {
        payments = payments.Where(item => item.CreatedAtUtc >= filter.FromUtc && item.CreatedAtUtc < filter.ToExclusiveUtc);
        if (filter.PaymentProviderId.HasValue)
            payments = payments.Where(item => item.PaymentProviderId == filter.PaymentProviderId.Value);
        if (filter.CurrencyId.HasValue)
            payments = payments.Where(item => item.CurrencyId == filter.CurrencyId.Value);
        if (filter.PaymentIntent.HasValue)
            payments = payments.Where(item => item.PaymentIntent == filter.PaymentIntent.Value);
        if (filter.PaymentStatus.HasValue)
            payments = payments.Where(item => item.PaymentStatus == filter.PaymentStatus.Value);
        if (filter.TenantId.HasValue)
            payments = payments.Where(item => item.TenantId == filter.TenantId.Value);
        return payments;
    }

    private sealed class TimelineAggregate<TKey> where TKey : notnull
    {
        public required TKey Period { get; init; }
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public long TransactionCount { get; init; }
        public long SuccessfulCount { get; init; }
        public long UnsuccessfulCount { get; init; }
        public long ProcessingCount { get; init; }
        public decimal AttemptedVolume { get; init; }
        public decimal SuccessfulVolume { get; init; }
        public decimal FailedVolume { get; init; }
        public decimal ProcessingVolume { get; init; }
        public decimal CancelledVolume { get; init; }
        public decimal ExpiredVolume { get; init; }
    }

    private sealed class TimelineSource<TKey> where TKey : notnull
    {
        public required PaymentTransaction Payment { get; init; }
        public required TKey Period { get; init; }
    }

    private sealed record DayPeriod(int Year, int Month, int Day);
    private sealed record MonthPeriod(int Year, int Month);
}
