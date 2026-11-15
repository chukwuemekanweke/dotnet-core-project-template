namespace BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;

public enum AdministrativelyCancelSubscriptionAtPeriodEndStatus
{
    Scheduled = 1,
    AlreadyScheduled = 2,
    NotFound = 3,
    InvalidState = 4,
    ConcurrencyConflict = 5
}
