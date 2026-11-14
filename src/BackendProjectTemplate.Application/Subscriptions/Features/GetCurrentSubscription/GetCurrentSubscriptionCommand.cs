using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;

public sealed record GetCurrentSubscriptionCommand(
    BillingAccountOwnerType BillingAccountOwnerType,
    ActorContext ActorContext);
