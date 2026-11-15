namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;

public enum CancelSubscriptionImmediatelyStatus
{
    Cancelled = 1,
    AlreadyCancelled = 2,
    NotFound = 3,
    InvalidState = 4,
    ConcurrencyConflict = 5
}
