namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;

public sealed record ListSubscriptionPlansResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<string> Features,
    IReadOnlyList<SubscriptionPlanPriceResponse> Prices,
    Guid CurrencyId,
    string CurrencyCode);
