namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionPlanReadModel(
    Guid PlanId,
    string Code,
    string Name,
    decimal Price,
    Guid CurrencyId,
    string CurrencyCode,
    BillingInterval BillingInterval,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
