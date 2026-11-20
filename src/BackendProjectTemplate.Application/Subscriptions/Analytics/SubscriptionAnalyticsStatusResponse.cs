namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsStatusResponse(
    string Status,
    long SubscriptionCount,
    decimal Percentage);
