namespace BackendProjectTemplate.Domain.Subscriptions;

public enum SubscriptionRenewalStatus
{
    Pending = 1,
    AwaitingAuthorization = 2,
    Charging = 3,
    AwaitingReconciliation = 4,
    RetryScheduled = 5,
    Fulfilled = 6,
    Exhausted = 7,
    Cancelled = 8
}
