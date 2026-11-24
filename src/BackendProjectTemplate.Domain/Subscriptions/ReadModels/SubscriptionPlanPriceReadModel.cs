namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionPlanPriceReadModel(
    BillingInterval BillingInterval,
    decimal Price);
