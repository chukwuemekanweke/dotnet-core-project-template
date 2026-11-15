using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionListRequest(
    Guid? TenantId,
    Guid? BillingAccountId,
    BillingAccountOwnerType? BillingAccountOwnerType,
    Guid? StakeholderId,
    Guid? PlanId,
    SubscriptionStatus? Status,
    SubscriptionAllocationSource? AllocationSource,
    bool? AutoRenew,
    bool? CancelAtPeriodEnd,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc,
    DateTimeOffset? CurrentPeriodFromUtc,
    DateTimeOffset? CurrentPeriodToUtc,
    DateTimeOffset? CursorCreatedAtUtc,
    Guid? CursorSubscriptionId,
    int Limit);
