namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed record AllocateSubscriptionRequest(
    Guid TenantId,
    string BillingAccountOwnerType,
    Guid? StakeholderId,
    Guid PlanId,
    DateTimeOffset? StartAtUtc,
    DateTimeOffset? EndAtUtc,
    string AllocationSource,
    string Reason,
    Guid IdempotencyKey,
    bool? AutoRenew = null,
    string? BillingInterval = null);
