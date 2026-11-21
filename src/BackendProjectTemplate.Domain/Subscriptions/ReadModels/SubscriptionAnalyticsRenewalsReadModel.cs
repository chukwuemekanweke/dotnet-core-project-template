namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsRenewalsReadModel(
    long SuccessfulRenewals,
    long FailedRenewals,
    long FailedPaymentAttempts,
    long PendingRenewals,
    long AwaitingReconciliationRenewals);
