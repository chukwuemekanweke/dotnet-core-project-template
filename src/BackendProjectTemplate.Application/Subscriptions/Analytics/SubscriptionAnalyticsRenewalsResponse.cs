namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsRenewalsResponse(
    long SuccessfulRenewals,
    long FailedRenewals,
    long FailedPaymentAttempts,
    long PendingRenewals,
    long AwaitingReconciliationRenewals,
    decimal SuccessRate);
