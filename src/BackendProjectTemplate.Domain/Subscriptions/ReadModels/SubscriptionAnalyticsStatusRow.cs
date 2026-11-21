namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsStatusRow(
    SubscriptionStatus Status,
    long SubscriptionCount);
