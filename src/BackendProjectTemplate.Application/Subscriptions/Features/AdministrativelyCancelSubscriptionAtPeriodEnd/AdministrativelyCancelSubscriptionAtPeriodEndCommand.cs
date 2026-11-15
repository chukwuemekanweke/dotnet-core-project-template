using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;

public sealed record AdministrativelyCancelSubscriptionAtPeriodEndCommand(
    Guid SubscriptionId,
    Guid TenantId,
    string Reason,
    ActorContext ActorContext);
