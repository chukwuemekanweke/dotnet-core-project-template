using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminSubscriptionSummary(
    Guid SubscriptionId,
    string PlanCode,
    SubscriptionStatus Status,
    DateTimeOffset? EntitlementEndUtc);
