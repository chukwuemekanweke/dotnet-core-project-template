namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsTimelineBucketResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    long ActiveSubscriptions,
    long NewSubscriptions,
    long ChurnedSubscriptions,
    long SuccessfulRenewals,
    long FailedRenewals,
    IReadOnlyList<SubscriptionAnalyticsRevenueResponse> Revenues);
