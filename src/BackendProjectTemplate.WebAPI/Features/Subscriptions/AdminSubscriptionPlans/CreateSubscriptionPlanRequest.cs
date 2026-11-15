namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed record CreateSubscriptionPlanRequest(
    string Code,
    string Name,
    decimal Price,
    Guid CurrencyId,
    string BillingInterval,
    bool IsActive = true);
