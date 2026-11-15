using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public static class AdminSubscriptionLifecycleMapper
{
    public static AdminSubscriptionLifecycleResponse ToResponse(Subscription subscription) =>
        new(
            subscription.Id,
            subscription.Status.ToString(),
            subscription.AutoRenew,
            subscription.CancelAtPeriodEnd,
            subscription.CurrentPeriodEndUtc,
            subscription.EntitlementEndUtc,
            subscription.CancellationRequestedAtUtc,
            subscription.CancelledAtUtc,
            subscription.EndedAtUtc,
            subscription.CancellationReason);
}
