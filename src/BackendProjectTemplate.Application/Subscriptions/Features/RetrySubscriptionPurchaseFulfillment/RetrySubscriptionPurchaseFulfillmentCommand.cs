using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;

public sealed record RetrySubscriptionPurchaseFulfillmentCommand(
    Guid SubscriptionId,
    Guid TenantId,
    ActorContext ActorContext);
