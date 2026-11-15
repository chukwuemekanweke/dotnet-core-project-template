using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;

public sealed record ListSubscriptionsCommand(
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
    string? Cursor,
    int Limit);
