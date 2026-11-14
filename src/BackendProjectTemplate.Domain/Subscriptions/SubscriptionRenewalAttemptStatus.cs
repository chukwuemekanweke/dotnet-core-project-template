namespace BackendProjectTemplate.Domain.Subscriptions;

public enum SubscriptionRenewalAttemptStatus
{
    Pending = 1,
    Submitted = 2,
    Unknown = 3,
    Failed = 4,
    Succeeded = 5
}
