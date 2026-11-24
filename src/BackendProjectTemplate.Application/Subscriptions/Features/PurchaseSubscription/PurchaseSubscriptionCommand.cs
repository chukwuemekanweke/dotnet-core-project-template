using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public sealed record PurchaseSubscriptionCommand(
    Guid PlanId,
    BillingInterval BillingInterval,
    Guid PaymentProviderId,
    BillingAccountOwnerType BillingAccountOwnerType,
    bool AutoRenew,
    Guid IdempotencyKey,
    ActorContext ActorContext);
