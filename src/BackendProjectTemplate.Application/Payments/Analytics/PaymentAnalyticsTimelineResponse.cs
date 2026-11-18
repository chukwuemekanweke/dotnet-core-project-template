namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsTimelineResponse(
    string Interval,
    IReadOnlyList<PaymentAnalyticsTimelineBucketResponse> Buckets);
