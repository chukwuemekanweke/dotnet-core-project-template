using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanCommand(
    string Code,
    string Name,
    IReadOnlyDictionary<BillingInterval, decimal> Prices,
    Guid CurrencyId,
    bool IsActive,
    string? Description,
    IReadOnlyList<string> Features,
    ActorContext ActorContext);
