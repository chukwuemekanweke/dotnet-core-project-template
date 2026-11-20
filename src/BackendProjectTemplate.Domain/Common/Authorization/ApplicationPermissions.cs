namespace BackendProjectTemplate.Domain.Common.Authorization;

public static class ApplicationPermissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Manage = "users.manage";
        public const string AnalyticsRead = "users.analytics.read";
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
        public const string AnalyticsRead = "payments.analytics.read";
    }

    public static class Subscriptions
    {
        public const string Read = "subscriptions.read";
        public const string AnalyticsRead = "subscriptions.analytics.read";
        public const string Manage = "subscriptions.manage";
        public const string Allocate = "subscriptions.allocate";
        public const string ManagePlans = "subscriptions.plans.manage";
    }

    public static class Providers
    {
        public const string Manage = "providers.manage";
    }

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Users.Read,
        Users.Manage,
        Users.AnalyticsRead,
        Kyc.Read,
        Kyc.Manage,
        Payments.Read,
        Payments.Manage,
        Payments.AnalyticsRead,
        Subscriptions.Read,
        Subscriptions.AnalyticsRead,
        Subscriptions.Manage,
        Subscriptions.Allocate,
        Subscriptions.ManagePlans,
        Providers.Manage
    };
}
