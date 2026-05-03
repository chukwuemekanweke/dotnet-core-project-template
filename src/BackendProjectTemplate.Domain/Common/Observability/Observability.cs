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
    public const string FlowNamePropertyName = "flow_name";
    public const string StepNamePropertyName = "step_name";
    public const string OutcomePropertyName = "outcome";
    public const string FailureReasonPropertyName = "failure_reason";
    public const string ProviderPropertyName = "provider";
    public const string SourcePropertyName = "source";
    public const string PaymentReferencePropertyName = "payment_reference";
    public const string PaymentMethodPropertyName = "payment_method";
    public const string PaymentIntentPropertyName = "payment_intent";
    public const string AmountBucketPropertyName = "amount_bucket";
    public const string CurrencyIdPropertyName = "currency_id";
    public const string TerminalStatePropertyName = "terminal_state";
    public const string DeliveryAttemptPropertyName = "delivery_attempt";
    public const string RetryCountPropertyName = "retry_count";
    public const string IsDuplicatePropertyName = "is_duplicate";
    public const string ExceptionTypePropertyName = "exception_type";
    public const string DurationMsPropertyName = "duration_ms";

    public static class FlowNames
    {
        public const string Authentication = "authentication";
        public const string Payments = "payments";
        public const string Onboarding = "onboarding";
    }

    public static class StepNames
    {
        public const string PaymentInitiation = "payment_initiation";
        public const string PaymentInfoReturn = "payment_info_return";
        public const string WebhookReceipt = "webhook_receipt";
        public const string WebhookProcessing = "webhook_processing";
        public const string PaymentStatusConfirmation = "payment_status_confirmation";
        public const string PaymentEventPublish = "payment_event_publish";
        public const string SubscriberProcessing = "subscriber_processing";
        public const string ValueGrant = "value_grant";
        public const string PaymentReconciliation = "payment_reconciliation";
    }

    public static class Outcomes
    {
        public const string Started = "started";
        public const string Success = "success";
        public const string Failure = "failure";
        public const string Timeout = "timeout";
        public const string Retry = "retry";
        public const string Duplicate = "duplicate";
        public const string Ignored = "ignored";
    }

    public static class Sources
    {
        public const string Api = "api";
        public const string Webhook = "webhook";
        public const string Subscriber = "subscriber";
        public const string Cron = "cron";
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
            public const string InfoReturned = "payment.info_returned";
            public const string WebhookReceived = "payment.webhook.received";
            public const string WebhookDuplicate = "payment.webhook.duplicate";
            public const string WebhookInvalidSignature = "payment.webhook.invalid_signature";
            public const string WebhookProcessed = "payment.webhook.processed";
            public const string WebhookProcessingFailed = "payment.webhook.processing_failed";
            public const string StatusConfirmed = "payment.status.confirmed";
            public const string StatusFailed = "payment.status.failed";
            public const string EventPublished = "payment.event.published";
            public const string EventPublishFailed = "payment.event.publish_failed";
            public const string SubscriberStarted = "payment.subscriber.started";
            public const string SubscriberSucceeded = "payment.subscriber.succeeded";
            public const string SubscriberFailed = "payment.subscriber.failed";
            public const string ValueGranted = "payment.value_granted";
            public const string ValueGrantFailed = "payment.value_grant_failed";
            public const string TimeoutNoWebhook = "payment.timeout.no_webhook";
            public const string ReconciliationChecked = "payment.reconciliation.checked";
            public const string ReconciliationConfirmed = "payment.reconciliation.confirmed";
            public const string ReconciliationFailed = "payment.reconciliation.failed";
            public const string ReconciliationStillPending = "payment.reconciliation.still_pending";
        }
    }
}
