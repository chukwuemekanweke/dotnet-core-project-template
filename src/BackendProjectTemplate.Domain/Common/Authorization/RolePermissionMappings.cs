namespace BackendProjectTemplate.Domain.Common.Authorization;

public static class RolePermissionMappings
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Mappings =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [ApplicationRoles.Administrator] = ApplicationPermissions.All,
            [ApplicationRoles.ComplianceOfficer] = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationPermissions.Users.Read,
                ApplicationPermissions.Kyc.Read,
                ApplicationPermissions.Kyc.Manage
            },
            [ApplicationRoles.SupportAgent] = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationPermissions.Users.Read,
                ApplicationPermissions.Subscriptions.Read
            },
            [ApplicationRoles.FinanceOfficer] = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationPermissions.Payments.Read,
                ApplicationPermissions.Payments.Manage,
                ApplicationPermissions.Subscriptions.Read,
                ApplicationPermissions.Subscriptions.Manage,
                ApplicationPermissions.Subscriptions.Allocate
            }
        };

    public static IReadOnlySet<string> ForRole(string roleName) =>
        Mappings.TryGetValue(roleName, out var permissions)
            ? permissions
            : new HashSet<string>(StringComparer.Ordinal);

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roleNames) =>
        roleNames
            .SelectMany(ForRole)
            .ToHashSet(StringComparer.Ordinal);
}
