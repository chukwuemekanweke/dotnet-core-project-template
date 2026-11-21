using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

internal static class SubscriptionAnalyticsHandlerSupport
{
    public static (SubscriptionAnalyticsFilter Filter, DateOnly From, DateOnly To) Resolve(
        SubscriptionAnalyticsQuery query,
        TimeProvider timeProvider,
        SubscriptionAnalyticsOptions options)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var to = query.To ?? today;
        var from = query.From ?? to.AddDays(-(options.DefaultRangeDays - 1));
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var toExclusiveUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        return (new SubscriptionAnalyticsFilter(
            fromUtc,
            toExclusiveUtc,
            toExclusiveUtc.AddTicks(-1),
            query.PlanId,
            query.OwnerType,
            query.TenantId,
            query.CurrencyId), from, to);
    }

    public static IReadOnlyList<SubscriptionAnalyticsRevenueResponse> MapRevenues(
        IReadOnlyList<SubscriptionAnalyticsRevenueRow> rows) =>
        rows.Select(row => new SubscriptionAnalyticsRevenueResponse(
            row.CurrencyId,
            row.CurrencyCode,
            row.Mrr,
            row.Mrr * 12m)).ToArray();
}
