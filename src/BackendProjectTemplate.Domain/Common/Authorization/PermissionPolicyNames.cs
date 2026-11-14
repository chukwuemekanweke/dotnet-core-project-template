namespace BackendProjectTemplate.Domain.Common.Authorization;

public static class PermissionPolicyNames
{
    private const string Prefix = "permission:";

    public const string UsersRead = Prefix + ApplicationPermissions.Users.Read;
    public const string UsersManage = Prefix + ApplicationPermissions.Users.Manage;
    public const string KycRead = Prefix + ApplicationPermissions.Kyc.Read;
    public const string KycManage = Prefix + ApplicationPermissions.Kyc.Manage;
    public const string PaymentsRead = Prefix + ApplicationPermissions.Payments.Read;
    public const string PaymentsManage = Prefix + ApplicationPermissions.Payments.Manage;
    public const string SubscriptionsRead = Prefix + ApplicationPermissions.Subscriptions.Read;
    public const string SubscriptionsManage = Prefix + ApplicationPermissions.Subscriptions.Manage;
    public const string SubscriptionsAllocate = Prefix + ApplicationPermissions.Subscriptions.Allocate;
    public const string ProvidersManage = Prefix + ApplicationPermissions.Providers.Manage;

    public static string For(string permission) =>
        ApplicationPermissions.All.Contains(permission)
            ? Prefix + permission
            : throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unknown application permission.");
}
