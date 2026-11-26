namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// Resource types an administrative audit record can target, and whether each type belongs to a tenant.
/// A tenant-scoped target must always carry the affected tenant; a global target must never carry one, so a
/// null target tenant always means a deliberately global operation rather than an unresolved tenant.
/// </summary>
public static class AdministrativeAuditTargetTypes
{
    /// <summary>An ASP.NET Identity user. Identity users are platform-wide, so user targets are global.</summary>
    public const string User = "User";

    /// <summary>An authentication session, scoped to the tenant of the stakeholder that owns it.</summary>
    public const string AuthenticationSession = "AuthenticationSession";

    public const string Subscription = "Subscription";
    public const string SubscriptionPlan = "SubscriptionPlan";
    public const string Provider = "Provider";
    public const string PaymentProvider = "PaymentProvider";

    private static readonly IReadOnlyDictionary<string, bool> TenantScopedByType =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [User] = false,
            [AuthenticationSession] = true,
            [Subscription] = true,
            [SubscriptionPlan] = false,
            [Provider] = false,
            [PaymentProvider] = false
        };

    public static IReadOnlySet<string> All { get; } = TenantScopedByType.Keys.ToHashSet(StringComparer.Ordinal);

    public static bool IsKnown(string? targetType) => targetType is not null && TenantScopedByType.ContainsKey(targetType);

    public static bool IsTenantScoped(string targetType) =>
        TenantScopedByType.TryGetValue(targetType, out var tenantScoped)
            ? tenantScoped
            : throw new ArgumentOutOfRangeException(nameof(targetType), targetType, "Unknown administrative audit target type.");
}
