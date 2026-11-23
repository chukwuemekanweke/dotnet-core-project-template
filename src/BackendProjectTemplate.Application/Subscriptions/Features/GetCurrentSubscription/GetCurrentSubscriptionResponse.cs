namespace BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;

public sealed record GetCurrentSubscriptionResponse(
    Guid Id,
    Guid BillingAccountId,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    decimal PlanPrice,
    Guid CurrencyId,
    string CurrencyCode,
    string BillingInterval,
    string Status,
    string LifecycleStatus,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? NextBillingAtUtc,
    bool AutoRenew,
    bool CancelAtPeriodEnd,
    DateTimeOffset? CancellationRequestedAtUtc,
    DateTimeOffset? CancellationEffectiveAtUtc);
