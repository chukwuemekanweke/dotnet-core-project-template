namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

/// <summary>
/// Replaces the plan's editable terms. Every currently offered interval must be included and new intervals may be
/// added; an omitted description or feature list clears it.
/// </summary>
public sealed record UpdateSubscriptionPlanRequest(
    string Name,
    IReadOnlyList<SubscriptionPlanPriceRequest> Prices,
    string? Description = null,
    IReadOnlyList<string>? Features = null);
