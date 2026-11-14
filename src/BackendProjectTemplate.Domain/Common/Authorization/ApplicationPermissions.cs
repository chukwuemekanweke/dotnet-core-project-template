namespace BackendProjectTemplate.Domain.Common.Authorization;

public static class ApplicationPermissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Manage = "users.manage";
    }

    public static class Kyc
    {
        public const string Read = "kyc.read";
        public const string Manage = "kyc.manage";
    }

    public static class Payments
    {
        public const string Read = "payments.read";
        public const string Manage = "payments.manage";
    }

    public static class Subscriptions
    {
        public const string Read = "subscriptions.read";
        public const string Manage = "subscriptions.manage";
        public const string Allocate = "subscriptions.allocate";
    }

    public static class Providers
    {
        public const string Manage = "providers.manage";
    }

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Users.Read,
        Users.Manage,
        Kyc.Read,
        Kyc.Manage,
        Payments.Read,
        Payments.Manage,
        Subscriptions.Read,
        Subscriptions.Manage,
        Subscriptions.Allocate,
        Providers.Manage
    };
}
