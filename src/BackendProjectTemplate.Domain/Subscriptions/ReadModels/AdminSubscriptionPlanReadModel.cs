namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionPlanReadModel(
    Guid PlanId,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<string> Features,
    IReadOnlyList<SubscriptionPlanPriceReadModel> Prices,
    Guid CurrencyId,
    string CurrencyCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
