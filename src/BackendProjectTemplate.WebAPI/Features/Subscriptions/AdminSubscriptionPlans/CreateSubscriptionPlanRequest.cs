namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed record CreateSubscriptionPlanRequest(
    string Code,
    string Name,
    IReadOnlyList<SubscriptionPlanPriceRequest> Prices,
    Guid CurrencyId,
    bool IsActive = true,
    string? Description = null,
    IReadOnlyList<string>? Features = null);
