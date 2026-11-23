using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;

public sealed record GetSubscriptionPurchaseStatusCommand(
    Guid PurchaseId,
    BillingAccountOwnerType BillingAccountOwnerType,
    ActorContext ActorContext);
