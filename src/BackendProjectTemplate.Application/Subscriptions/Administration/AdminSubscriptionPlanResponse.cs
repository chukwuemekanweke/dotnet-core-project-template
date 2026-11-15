namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public sealed record AdminSubscriptionPlanResponse(
    Guid Id,
    string Code,
    string Name,
    decimal Price,
    Guid CurrencyId,
    string CurrencyCode,
    string BillingInterval,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
