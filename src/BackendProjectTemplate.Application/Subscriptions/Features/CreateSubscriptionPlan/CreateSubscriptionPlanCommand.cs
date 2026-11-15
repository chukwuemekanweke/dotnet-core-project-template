using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanCommand(
    string Code,
    string Name,
    decimal Price,
    Guid CurrencyId,
    BillingInterval BillingInterval,
    bool IsActive,
    ActorContext ActorContext);
