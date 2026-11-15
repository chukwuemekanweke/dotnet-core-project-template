using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

public sealed record AllocateSubscriptionCommand(
    Guid TenantId,
    BillingAccountOwnerType BillingAccountOwnerType,
    Guid? StakeholderId,
    Guid PlanId,
    DateTimeOffset? StartAtUtc,
    DateTimeOffset? EndAtUtc,
    SubscriptionAllocationSource AllocationSource,
    string Reason,
    Guid IdempotencyKey,
    ActorContext ActorContext);
