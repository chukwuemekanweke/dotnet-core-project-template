namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public sealed record AdminSubscriptionPlanResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<string> Features,
    IReadOnlyList<SubscriptionPlanPriceResponse> Prices,
    Guid CurrencyId,
    string CurrencyCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
