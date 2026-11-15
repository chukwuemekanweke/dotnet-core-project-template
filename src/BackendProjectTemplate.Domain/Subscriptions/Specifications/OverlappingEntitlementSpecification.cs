using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

/// <summary>
/// Finds Scheduled or Active subscriptions on a BillingAccount whose half-open entitlement window
/// <c>[EntitlementStartUtc, EntitlementEndUtc)</c> intersects <c>[startUtc, endUtc)</c>. A <c>null</c> end on
/// either side means open-ended. Mirrors the PostgreSQL exclusion constraint so the application can reject
/// conflicts deterministically; the constraint remains the authoritative concurrent-write guard.
/// </summary>
public sealed class OverlappingEntitlementSpecification : Specification<Subscription>
{
    public OverlappingEntitlementSpecification(
        Guid tenantId,
        Guid billingAccountId,
        DateTimeOffset startUtc,
        DateTimeOffset? endUtc,
        Guid? excludedSubscriptionId)
    {
        Where(subscription =>
            subscription.TenantId == tenantId &&
            subscription.BillingAccountId == billingAccountId &&
            (subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.Scheduled) &&
            subscription.EntitlementStartUtc != null &&
            (excludedSubscriptionId == null || subscription.Id != excludedSubscriptionId) &&
            (endUtc == null || subscription.EntitlementStartUtc < endUtc) &&
            (subscription.EntitlementEndUtc == null || subscription.EntitlementEndUtc > startUtc));
    }
}
