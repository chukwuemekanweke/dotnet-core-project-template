namespace BackendProjectTemplate.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "BackendProjectTemplate";
    public const string FlowIdHeaderName = "X-Flow-Id";

    public const string MessageTypePropertyName = "message_type";
    public const string MessageIdPropertyName = "message_id";
    public const string UserIdPropertyName = "user_id";
    public const string StakeholderIdPropertyName = "stakeholder_id";
    public const string TenantIdPropertyName = "tenant_id";
    public const string CorrelationIdPropertyName = "correlation_id";
    public const string FlowIdPropertyName = "flow.id";
    public const string FailureReasonPropertyName = "failure_reason";
    public const string ProviderPropertyName = "provider";
    public const string PaymentReferencePropertyName = "payment_reference";
    public const string PaymentMethodPropertyName = "payment_method";
    public const string PaymentIntentPropertyName = "payment_intent";
    public const string CurrencyIdPropertyName = "currency_id";
    public const string TerminalStatePropertyName = "terminal_state";
    public const string ExceptionTypePropertyName = "exception_type";

    public static class EventNames
    {
        public static class Authentication
        {
            public const string PasswordSignUpStarted = "PasswordSignUpStarted";
            public const string PasswordSignUpCompleted = "PasswordSignUpCompleted";
            public const string PasswordSignUpFailed = "PasswordSignUpFailed";
            public const string GoogleSignUpStarted = "GoogleSignUpStarted";
            public const string GoogleSignUpCompleted = "GoogleSignUpCompleted";
            public const string GoogleSignUpFailed = "GoogleSignUpFailed";
            public const string EmailConfirmationOtpSent = "EmailConfirmationOtpSent";
            public const string EmailConfirmationStarted = "EmailConfirmationStarted";
            public const string EmailConfirmationCompleted = "EmailConfirmationCompleted";
            public const string EmailConfirmationFailed = "EmailConfirmationFailed";
            public const string PasswordSignInStarted = "PasswordSignInStarted";
            public const string PasswordSignInCompleted = "PasswordSignInCompleted";
            public const string GoogleSignInStarted = "GoogleSignInStarted";
            public const string GoogleSignInCompleted = "GoogleSignInCompleted";
            public const string SignInPostProcessingCompleted = "SignInPostProcessingCompleted";
            public const string SignInFailureProcessed = "SignInFailureProcessed";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string PasswordResetRequestFailed = "PasswordResetRequestFailed";
            public const string PasswordResetOtpSent = "PasswordResetOtpSent";
            public const string PasswordResetCompleted = "PasswordResetCompleted";
            public const string PasswordResetCompletionFailed = "PasswordResetCompletionFailed";
            public const string SignOutCompleted = "SignOutCompleted";
            public const string SessionRefreshCompleted = "SessionRefreshCompleted";
            public const string SessionRefreshPostProcessingCompleted = "SessionRefreshPostProcessingCompleted";
            public const string ProfileUpdateCompleted = "ProfileUpdateCompleted";
            public const string ProfileUpdateFailed = "ProfileUpdateFailed";
            public const string AvatarUploadCompleted = "AvatarUploadCompleted";
            public const string AvatarUploadFailed = "AvatarUploadFailed";
        }

        public static class Notifications
        {
            public const string EmailSent = "EmailNotificationSent";
        }

        public static class Payments
        {
            public const string Initiated = "payment.initiated";
            public const string InitiationFailed = "payment.initiation_failed";
            public const string WebhookReceived = "payment.webhook.received";
            public const string WebhookPersisted = "payment.webhook.persisted";
            public const string WebhookPersistenceFailed = "payment.webhook.persistence_failed";
            public const string ReconciliationConfirmed = "payment.reconciliation.confirmed";
            public const string ReconciliationFailed = "payment.reconciliation.failed";
            public const string SubscriptionActivated = "payment.subscription.activated";
            public const string WalletCreated = "payment.wallet.created";
            public const string WalletCredited = "payment.wallet.credited";
            public const string CreditWallet = "payment.credit_wallet";
            public const string ActivateSubscription = "payment.activate_subscription";
        }
    }
}
