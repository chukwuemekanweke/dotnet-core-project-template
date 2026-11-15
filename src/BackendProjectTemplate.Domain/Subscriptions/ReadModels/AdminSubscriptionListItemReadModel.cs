using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionListItemReadModel(
    Guid SubscriptionId,
    Guid TenantId,
    string TenantName,
    Guid BillingAccountId,
    BillingAccountOwnerType BillingAccountOwnerType,
    Guid? StakeholderId,
    string BillingAccountDisplayName,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    SubscriptionStatus Status,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? EntitlementStartUtc,
    DateTimeOffset? EntitlementEndUtc,
    DateTimeOffset? NextBillingAtUtc,
    bool AutoRenew,
    bool CancelAtPeriodEnd,
    DateTimeOffset CreatedAtUtc,
    SubscriptionOrigin Origin,
    SubscriptionAllocationSource? AllocationSource);
