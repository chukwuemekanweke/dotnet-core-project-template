namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed record SubscriptionPlanPriceRequest(string BillingInterval, decimal Price);
