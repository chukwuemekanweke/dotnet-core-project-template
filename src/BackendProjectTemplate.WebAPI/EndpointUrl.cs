namespace BackendProjectTemplate.WebAPI;

public static class EndpointUrl
{
    public static class Versions
    {
        public const string V1 = "v1";
        public const string V1Route = "v{version:apiVersion}";
    }

    public static class Registrations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/registrations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class EmailConfirmations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-confirmations";
        public static readonly string V1 = ToV1(Route);
        public static readonly string ConfirmationCodeV1 = $"{V1}/confirmation-code";
    }

    public static class EmailExistenceChecks
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-existence-checks";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Sessions
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sessions";
        public static readonly string V1 = ToV1(Route);
        public static readonly string GoogleV1 = $"{V1}/google";
        public static readonly string RefreshV1 = $"{V1}/refresh";
        public static readonly string LogoutV1 = $"{V1}/logout";
        public static readonly string TwoFactorV1 = $"{V1}/two-factor";
    }

    public static class AuthenticationSecurity
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/security";
        public static readonly string V1 = ToV1(Route);
        public static readonly string TwoFactorV1 = $"{V1}/two-factor";
    }

    public static class Passkeys
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/passkeys";
        public static readonly string V1 = ToV1(Route);
        public static readonly string RegistrationOptionsV1 = $"{V1}/registration-options";
        public static readonly string RegistrationsV1 = $"{V1}/registrations";
        public static readonly string AuthenticationOptionsV1 = $"{V1}/authentication-options";
        public static readonly string AuthenticationCompletionsV1 = $"{V1}/authentication-completions";
    }

    public static class GoogleRegistrations
    {
        public const string Route = $"{Registrations.Route}/google";
        public static readonly string V1 = ToV1(Route);
    }

    public static class GoogleAuthenticationFlows
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/google/flows";
        public static readonly string V1 = ToV1(Route);
    }

    public static class GoogleLinks
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/google-links";
        public static readonly string V1 = ToV1(Route);
    }

    public static class PasswordResets
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/password-resets";
        public static readonly string V1 = ToV1(Route);
        public static readonly string CompletionsV1 = $"{V1}/completions";
    }

    public static class Passwords
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/password";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Countries
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/countries";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Languages
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/languages";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Stakeholders
    {
        public const string Route = $"api/{Versions.V1Route}/stakeholders";
        public static readonly string V1 = ToV1(Route);
        public static readonly string LoginActivityV1 = $"{V1}/me/login-activity";
    }

    public static class Providers
    {
        public const string Route = $"api/{Versions.V1Route}/providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Payments
    {
        public const string Route = $"api/{Versions.V1Route}/payments";
        public static readonly string V1 = ToV1(Route);
        public static readonly string InitiateV1 = $"{V1}/initiate";
        public static readonly string WalletTransactionsV1 = $"{V1}/wallet-transactions";
        public static string WalletTopUpTransactionDetailsV1(Guid walletTransactionId) =>
            $"{WalletTransactionsV1}/top-ups/{walletTransactionId}";
    }

    public static class PaymentProviders
    {
        public const string Route = $"api/{Versions.V1Route}/payment-providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class SubscriptionPlans
    {
        public const string Route = $"api/{Versions.V1Route}/subscription-plans";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Subscriptions
    {
        public const string Route = $"api/{Versions.V1Route}/subscriptions";
        public static readonly string V1 = ToV1(Route);
        public static readonly string PurchasesV1 = $"{V1}/purchases";
        public static readonly string CurrentV1 = $"{V1}/current";
        public static readonly string CurrentCancellationV1 = $"{CurrentV1}/cancellation-at-period-end";
    }

    public static class PaymentWebhooks
    {
        public static class Bachs
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/bachs";
            public static readonly string V1 = ToV1(Route);
        }

        public static class SafeHaven
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/safehaven";
            public static readonly string V1 = ToV1(Route);
        }

        public static class Credo
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/credo";
            public static readonly string V1 = ToV1(Route);
        }
    }

    public static class EmailNotificationWebhooks
    {
        public static class Mailtrap
        {
            public const string Route = $"api/{Versions.V1Route}/email-notifications/webhooks/mailtrap";
            public static readonly string V1 = ToV1(Route);
        }
    }

    private static string ToV1(string routeTemplate) =>
        routeTemplate.Replace(Versions.V1Route, Versions.V1, StringComparison.Ordinal);
}
