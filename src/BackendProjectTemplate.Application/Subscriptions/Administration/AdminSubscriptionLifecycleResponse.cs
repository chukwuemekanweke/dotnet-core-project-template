namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public sealed record AdminSubscriptionLifecycleResponse(
    Guid SubscriptionId,
    string Status,
    bool AutoRenew,
    bool CancelAtPeriodEnd,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? EntitlementEndUtc,
    DateTimeOffset? CancellationRequestedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? EndedAtUtc,
    string? CancellationReason);
