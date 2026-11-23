using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListEligiblePaymentProviders;

public sealed record ListEligiblePaymentProvidersCommand(
    Guid PlanId,
    BillingAccountOwnerType BillingAccountOwnerType,
    ActorContext ActorContext);
