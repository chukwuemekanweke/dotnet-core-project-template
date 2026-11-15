namespace BackendProjectTemplate.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "BackendProjectTemplate";
    public const string FlowIdHeaderName = "X-Flow-Id";

    public static class PropertyNames
    {
        public static class Common
        {
            public const string MessageType = "message_type";
            public const string MessageId = "message_id";
            public const string OccurredAt = "occurred_at";
            public const string RequestedAt = "requested_at";
            public const string UserId = "user_id";
            public const string StakeholderId = "stakeholder_id";
            public const string TenantId = "tenant_id";
            public const string CorrelationId = "correlation_id";
            public const string FlowId = "flow.id";
            public const string FailureReason = "failure_reason";
            public const string ExceptionType = "exception_type";
            public const string UploadId = "upload_id";
            public const string ContentType = "content_type";
            public const string ContentLength = "content_length";
            public const string ExpiresAtUtc = "expires_at_utc";
            public const string QueueDelayMilliseconds = "queue_delay_ms";
            public const string RemainingLifetimeMilliseconds = "remaining_lifetime_ms";
        }

        public static class Notifications
        {
            public const string ProviderKey = "provider_key";
            public const string ProviderMessageId = "provider_message_id";
            public const string NotificationType = "notification_type";
        }

        public static class Payments
        {
            public const string Provider = "provider";
            public const string PaymentReference = "payment_reference";
            public const string MerchantReference = "merchant_reference";
            public const string ProviderReference = "provider_reference";
            public const string PaymentMethod = "payment_method";
            public const string PaymentIntent = "payment_intent";
            public const string CurrencyId = "currency_id";
            public const string CurrencyCode = "currency_code";
            public const string WalletId = "wallet_id";
            public const string TerminalState = "terminal_state";
        }

        public static class Subscriptions
        {
            public const string SubscriptionId = "subscription_id";
            public const string PurchaseId = "subscription_purchase_id";
            public const string PlanId = "subscription_plan_id";
            public const string BillingAccountId = "billing_account_id";
            public const string RenewalId = "subscription_renewal_id";
            public const string AllocationId = "subscription_allocation_id";
            public const string AllocationSource = "subscription_allocation_source";
            public const string OperationId = "subscription_operation_id";
            public const string Status = "subscription_status";
        }
    }

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
            public const string EmailConfirmationOtpSendSkipped = "EmailConfirmationOtpSendSkipped";
            public const string EmailConfirmationStarted = "EmailConfirmationStarted";
            public const string EmailConfirmationCompleted = "EmailConfirmationCompleted";
            public const string EmailConfirmationFailed = "EmailConfirmationFailed";
            public const string PasswordSignInStarted = "PasswordSignInStarted";
            public const string PasswordSignInCompleted = "PasswordSignInCompleted";
            public const string GoogleSignInStarted = "GoogleSignInStarted";
            public const string GoogleSignInCompleted = "GoogleSignInCompleted";
            public const string PasskeySignInStarted = "PasskeySignInStarted";
            public const string PasskeySignInCompleted = "PasskeySignInCompleted";
            public const string PasskeySignInFailed = "PasskeySignInFailed";
            public const string UnknownSignInCompleted = "UnknownSignInCompleted";
            public const string PasskeyRegistrationCompleted = "PasskeyRegistrationCompleted";
            public const string PasskeyRemoved = "PasskeyRemoved";
            public const string SignInPostProcessingCompleted = "SignInPostProcessingCompleted";
            public const string SignInFailureProcessed = "SignInFailureProcessed";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string PasswordResetRequestFailed = "PasswordResetRequestFailed";
            public const string PasswordResetOtpSent = "PasswordResetOtpSent";
            public const string PasswordResetCompleted = "PasswordResetCompleted";
            public const string PasswordResetCompletionFailed = "PasswordResetCompletionFailed";
            public const string PasswordChangeCompleted = "PasswordChangeCompleted";
            public const string PasswordChangeFailed = "PasswordChangeFailed";
            public const string SignOutCompleted = "SignOutCompleted";
            public const string SessionRefreshCompleted = "SessionRefreshCompleted";
            public const string SessionRefreshPostProcessingCompleted = "SessionRefreshPostProcessingCompleted";
            public const string TwoFactorChallengeRequired = "TwoFactorChallengeRequired";
            public const string TwoFactorVerificationCompleted = "TwoFactorVerificationCompleted";
            public const string TwoFactorVerificationFailed = "TwoFactorVerificationFailed";
            public const string TwoFactorEnrollmentCompleted = "TwoFactorEnrollmentCompleted";
            public const string TwoFactorDisabled = "TwoFactorDisabled";
            public const string TwoFactorRecoveryCodesRegenerated = "TwoFactorRecoveryCodesRegenerated";
            public const string ProfileUpdateCompleted = "ProfileUpdateCompleted";
            public const string ProfileUpdateFailed = "ProfileUpdateFailed";
            public const string AvatarUploadCompleted = "AvatarUploadCompleted";
            public const string AvatarUploadFailed = "AvatarUploadFailed";
            public const string AvatarUploadInitiated = "AvatarUploadInitiated";
        }

        public static class Notifications
        {
            public const string EmailSent = "EmailNotificationSent";
            public const string EmailDelivered = "EmailNotificationDelivered";
            public const string EmailBounced = "EmailNotificationBounced";
            public const string EmailRejected = "EmailNotificationRejected";
        }

        public static class Payments
        {
            public const string Initiated = "payment.initiated";
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

        public static class Subscriptions
        {
            public const string PurchaseInitiated = "subscription.purchase.initiated";
            public const string CancellationScheduled = "subscription.cancellation.scheduled";
            public const string RenewalAwaitingAuthorization = "subscription.renewal.awaiting_authorization";
            public const string RenewalReminderScheduled = "subscription.renewal.reminder_scheduled";
            public const string Expired = "subscription.expired";
            public const string Allocated = "subscription.allocation.created";
            public const string AllocationActivated = "subscription.allocation.activated";
            public const string AllocationActivationSkipped = "subscription.allocation.activation_skipped";
            public const string AllocationLapsed = "subscription.allocation.lapsed";
            public const string CancelledImmediately = "subscription.cancelled_immediately";
            public const string AdministrativeCancellationScheduled = "subscription.cancellation.administratively_scheduled";
            public const string PurchaseFulfillmentBlocked = "subscription.purchase.fulfillment_blocked";
            public const string PurchaseFulfillmentRetryRequested = "subscription.purchase.fulfillment_retry_requested";
            public const string RenewalFulfillmentBlocked = "subscription.renewal.fulfillment_blocked";
            public const string PlanCreated = "subscription.plan.created";
            public const string PlanUpdated = "subscription.plan.updated";
            public const string PlanActivationChanged = "subscription.plan.activation_changed";
        }
    }
}
