namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// Stable identifiers for successful privileged administrative mutations recorded in the administrative audit
/// journal. Identifiers are persisted, so never rename one: retire it by moving it to <see cref="Retired"/>
/// so historical records stay filterable.
/// </summary>
public static class AdministrativeAuditActions
{
    public static class Users
    {
        public const string Locked = "users.locked";
        public const string Unlocked = "users.unlocked";
        public const string RoleAssigned = "users.roles.assigned";
        public const string RoleRemoved = "users.roles.removed";
        public const string SessionRevoked = "users.sessions.revoked";
        public const string AllSessionsRevoked = "users.sessions.revoked-all";
    }

    public static class Subscriptions
    {
        public const string Allocated = "subscriptions.allocated";
        public const string Cancelled = "subscriptions.cancelled";
        public const string CancellationScheduled = "subscriptions.cancellation-scheduled";
        public const string PurchaseFulfillmentRetryRequested = "subscriptions.purchase-fulfillment-retry-requested";
    }

    public static class SubscriptionPlans
    {
        public const string Created = "subscription-plans.created";
        public const string Updated = "subscription-plans.updated";
        public const string Activated = "subscription-plans.activated";
        public const string Deactivated = "subscription-plans.deactivated";
    }

    public static class Providers
    {
        public const string Activated = "providers.activated";
        public const string Deactivated = "providers.deactivated";
    }

    public static class PaymentProviders
    {
        public const string Activated = "payment-providers.activated";
        public const string Deactivated = "payment-providers.deactivated";
    }

    /// <summary>Actions that may be recorded today.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Users.Locked,
        Users.Unlocked,
        Users.RoleAssigned,
        Users.RoleRemoved,
        Users.SessionRevoked,
        Users.AllSessionsRevoked,
        Subscriptions.Allocated,
        Subscriptions.Cancelled,
        Subscriptions.CancellationScheduled,
        Subscriptions.PurchaseFulfillmentRetryRequested,
        SubscriptionPlans.Created,
        SubscriptionPlans.Updated,
        SubscriptionPlans.Activated,
        SubscriptionPlans.Deactivated,
        Providers.Activated,
        Providers.Deactivated,
        PaymentProviders.Activated,
        PaymentProviders.Deactivated
    };

    /// <summary>
    /// Actions that are no longer recorded but may exist in historical records. They remain valid query
    /// filters and are never accepted for new records.
    /// </summary>
    public static IReadOnlySet<string> Retired { get; } = new HashSet<string>(StringComparer.Ordinal);

    public static bool IsRecordable(string? action) => action is not null && All.Contains(action);

    public static bool IsQueryable(string? action) =>
        action is not null && (All.Contains(action) || Retired.Contains(action));
}
