using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsFilter(
    DateTimeOffset FromUtc,
    DateTimeOffset ToExclusiveUtc,
    DateTimeOffset SnapshotUtc,
    Guid? PlanId,
    BillingAccountOwnerType? OwnerType,
    Guid? TenantId,
    Guid? CurrencyId);
