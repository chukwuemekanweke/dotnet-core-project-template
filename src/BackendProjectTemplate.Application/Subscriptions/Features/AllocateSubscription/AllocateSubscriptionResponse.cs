namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

public sealed record AllocateSubscriptionResponse(
    Guid SubscriptionId,
    Guid AllocationId,
    Guid TenantId,
    Guid BillingAccountId,
    Guid PlanId,
    string Status,
    string AllocationSource,
    DateTimeOffset EntitlementStartUtc,
    DateTimeOffset EntitlementEndUtc,
    bool AutoRenew);
