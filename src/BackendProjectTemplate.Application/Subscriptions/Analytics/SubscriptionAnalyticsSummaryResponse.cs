namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsSummaryResponse(
    DateOnly From,
    DateOnly To,
    DateTimeOffset SnapshotAtUtc,
    long ActiveSubscriptions,
    long NewSubscriptions,
    long ChurnedSubscriptions,
    long PastDueSubscriptions,
    long CancelAtPeriodEndSubscriptions,
    long SuccessfulRenewals,
    long FailedRenewals,
    decimal RenewalSuccessRate,
    IReadOnlyList<SubscriptionAnalyticsRevenueResponse> Revenues);
