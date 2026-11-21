using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? PlanId,
    BillingAccountOwnerType? OwnerType,
    Guid? TenantId,
    Guid? CurrencyId);
