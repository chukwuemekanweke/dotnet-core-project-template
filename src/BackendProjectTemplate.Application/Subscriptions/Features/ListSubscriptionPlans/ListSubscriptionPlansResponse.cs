namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;

public sealed record ListSubscriptionPlansResponse(
    Guid Id,
    string Code,
    string Name,
    decimal Price,
    Guid CurrencyId,
    string CurrencyCode,
    string BillingInterval);
