namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsSummaryReadModel(
    long ActiveSubscriptions,
    long NewSubscriptions,
    long ChurnedSubscriptions,
    long PastDueSubscriptions,
    long CancelAtPeriodEndSubscriptions,
    long SuccessfulRenewals,
    long FailedRenewals,
    IReadOnlyList<SubscriptionAnalyticsRevenueRow> Revenues);
