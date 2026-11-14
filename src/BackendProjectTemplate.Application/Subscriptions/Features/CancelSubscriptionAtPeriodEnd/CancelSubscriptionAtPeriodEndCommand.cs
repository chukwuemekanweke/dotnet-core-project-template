using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;

public sealed record CancelSubscriptionAtPeriodEndCommand(
    BillingAccountOwnerType BillingAccountOwnerType,
    ActorContext ActorContext);
