namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsTimelineRow(
    DateOnly PeriodStart,
    long ActiveSubscriptions,
    long NewSubscriptions,
    long ChurnedSubscriptions,
    long SuccessfulRenewals,
    long FailedRenewals,
    IReadOnlyList<SubscriptionAnalyticsRevenueRow> Revenues);
