namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsTimelineResponse(
    string Interval,
    IReadOnlyList<SubscriptionAnalyticsTimelineBucketResponse> Buckets);
