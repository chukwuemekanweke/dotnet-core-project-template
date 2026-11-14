using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public sealed record PurchaseSubscriptionCommand(
    Guid PlanId,
    Guid PaymentProviderId,
    BillingAccountOwnerType BillingAccountOwnerType,
    bool AutoRenew,
    ActorContext ActorContext);
