namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed record ListSubscriptionsRequest(
    Guid? TenantId = null,
    Guid? BillingAccountId = null,
    string? BillingAccountOwnerType = null,
    Guid? StakeholderId = null,
    Guid? PlanId = null,
    string? Status = null,
    string? AllocationSource = null,
    bool? AutoRenew = null,
    bool? CancelAtPeriodEnd = null,
    DateTimeOffset? CreatedFromUtc = null,
    DateTimeOffset? CreatedToUtc = null,
    DateTimeOffset? CurrentPeriodFromUtc = null,
    DateTimeOffset? CurrentPeriodToUtc = null,
    string? Cursor = null,
    int Limit = 20);
