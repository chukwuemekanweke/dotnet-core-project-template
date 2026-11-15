namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public sealed record AdminSubscriptionSummaryResponse(
    Guid SubscriptionId,
    Guid TenantId,
    string TenantName,
    Guid BillingAccountId,
    string BillingAccountOwnerType,
    Guid? StakeholderId,
    string BillingAccountDisplayName,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    string Status,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? EntitlementStartUtc,
    DateTimeOffset? EntitlementEndUtc,
    DateTimeOffset? NextBillingAtUtc,
    bool AutoRenew,
    bool CancelAtPeriodEnd,
    DateTimeOffset CreatedAtUtc,
    string Origin,
    string? AllocationSource);
