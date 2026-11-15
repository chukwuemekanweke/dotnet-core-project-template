namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

public enum AllocateSubscriptionStatus
{
    Allocated = 1,
    Replayed = 2,
    PlanNotFound = 3,
    PlanInactive = 4,
    OwnerNotFound = 5,
    OwnerTenantMismatch = 6,
    InvalidPeriod = 7,
    EntitlementConflict = 8,
    IdempotencyKeyReused = 9
}
