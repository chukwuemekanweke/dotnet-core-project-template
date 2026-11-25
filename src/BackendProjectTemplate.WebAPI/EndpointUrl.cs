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
        public static readonly string StepUpVerificationsV1 = $"{V1}/step-up/verifications";
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
        public static readonly string SecurityActivityV1 = $"{V1}/me/security-activity";
    }

    public static class Providers
    {
        public const string Route = $"api/{Versions.V1Route}/providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Kyc
    {
        public const string Route = $"api/{Versions.V1Route}/stakeholders/me/kyc";
        public static readonly string V1 = ToV1(Route);
        public static readonly string StartV1 = $"{V1}/start";
    }

    public static class KycWebhooks
    {
        public static class Kycaid
        {
            public const string Route = $"api/{Versions.V1Route}/webhooks/kycaid";
            public static readonly string V1 = ToV1(Route);
        }
    }

    public static class AdminKyc
    {
        public const string Route = $"api/{Versions.V1Route}/admin/kyc";
        public static readonly string V1 = ToV1(Route);
        public static readonly string SummaryV1 = $"{V1}/summary";
        public static string DetailV1(Guid verificationId) => $"{V1}/{verificationId}";
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
        public static string PurchaseStatusV1(Guid purchaseId) => $"{PurchasesV1}/{purchaseId}";
        public static readonly string BillingHistoryV1 = $"{V1}/billing-history";
        public static readonly string CurrentV1 = $"{V1}/current";
        public static readonly string CurrentCancellationV1 = $"{CurrentV1}/cancellation-at-period-end";
    }

    public static class AdminSubscriptions
    {
        public const string Route = $"api/{Versions.V1Route}/admin/subscriptions";
        public static readonly string V1 = ToV1(Route);
        public static readonly string AllocationsV1 = $"{V1}/allocations";
        public static string DetailV1(Guid subscriptionId) => $"{V1}/{subscriptionId}";
        public static string ImmediateCancellationV1(Guid subscriptionId) => $"{V1}/{subscriptionId}/immediate-cancellation";
        public static string CancellationAtPeriodEndV1(Guid subscriptionId) => $"{V1}/{subscriptionId}/cancellation-at-period-end";
        public static string PurchaseFulfillmentRetriesV1(Guid subscriptionId) => $"{V1}/{subscriptionId}/purchase-fulfillment-retries";
    }

    public static class AdminPayments
    {
        public const string Route = $"api/{Versions.V1Route}/admin/payments";
        public static readonly string V1 = ToV1(Route);
        public static string DetailV1(Guid paymentTransactionId) => $"{V1}/{paymentTransactionId}";
    }

    public static class AdminPaymentAnalytics
    {
        public const string Route = $"{AdminPayments.Route}/analytics";
        public static readonly string V1 = ToV1(Route);
        public static readonly string SummaryV1 = $"{V1}/summary";
        public static readonly string TimelineV1 = $"{V1}/timeline";
        public static readonly string StatusesV1 = $"{V1}/statuses";
        public static readonly string ProvidersV1 = $"{V1}/providers";
        public static readonly string IntentsV1 = $"{V1}/intents";
    }

    public static class AdminSubscriptionAnalytics
    {
        public const string Route = $"{AdminSubscriptions.Route}/analytics";
        public static readonly string V1 = ToV1(Route);
        public static readonly string SummaryV1 = $"{V1}/summary";
        public static readonly string TimelineV1 = $"{V1}/timeline";
        public static readonly string RenewalsV1 = $"{V1}/renewals";
        public static readonly string PlansV1 = $"{V1}/plans";
        public static readonly string StatusesV1 = $"{V1}/statuses";
    }

    public static class AdminUsers
    {
        public const string Route = $"api/{Versions.V1Route}/admin/users";
        public static readonly string V1 = ToV1(Route);
        public static string DetailV1(Guid userId) => $"{V1}/{userId}";
    }

    public static class AdminUserAnalytics
    {
        public const string Route = $"api/{Versions.V1Route}/admin/analytics";
        public static readonly string V1 = ToV1(Route);
        public static readonly string RegistrationsV1 = $"{V1}/registrations";
        public static readonly string ActiveUsersV1 = $"{V1}/active-users";
        public static readonly string RegistrationUsersV1 = $"{AdminUsers.V1}/registrations";
        public static readonly string ActiveUserListV1 = $"{AdminUsers.V1}/active";
    }

    public static class AdminSubscriptionPlans
    {
        public const string Route = $"api/{Versions.V1Route}/admin/subscription-plans";
        public static readonly string V1 = ToV1(Route);
        public static string PlanV1(Guid planId) => $"{V1}/{planId}";
        public static string ActivationV1(Guid planId) => $"{V1}/{planId}/activation";
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
