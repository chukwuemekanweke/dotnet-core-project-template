namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

public class AdminSubscriptionAnalyticsRequest
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public Guid? PlanId { get; init; }
    public string? OwnerType { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? CurrencyId { get; init; }
}
