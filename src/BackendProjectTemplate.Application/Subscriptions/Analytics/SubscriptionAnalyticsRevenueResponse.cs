namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsRevenueResponse(
    Guid CurrencyId,
    string CurrencyCode,
    decimal Mrr,
    decimal Arr);
