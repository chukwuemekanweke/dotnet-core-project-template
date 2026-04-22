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

    public static class EventNames
    {
        public static class Authentication
        {
            public const string PasswordSignUpStarted = "PasswordSignUpStarted";
            public const string PasswordSignUpCompleted = "PasswordSignUpCompleted";
            public const string GoogleSignUpStarted = "GoogleSignUpStarted";
            public const string GoogleSignUpCompleted = "GoogleSignUpCompleted";
            public const string EmailConfirmationOtpSent = "EmailConfirmationOtpSent";
            public const string EmailConfirmationStarted = "EmailConfirmationStarted";
            public const string EmailConfirmationCompleted = "EmailConfirmationCompleted";
            public const string PasswordSignInStarted = "PasswordSignInStarted";
            public const string PasswordSignInCompleted = "PasswordSignInCompleted";
            public const string GoogleSignInStarted = "GoogleSignInStarted";
            public const string GoogleSignInCompleted = "GoogleSignInCompleted";
            public const string SignInPostProcessingCompleted = "SignInPostProcessingCompleted";
            public const string SignInFailureProcessed = "SignInFailureProcessed";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string PasswordResetOtpSent = "PasswordResetOtpSent";
            public const string PasswordResetCompleted = "PasswordResetCompleted";
            public const string SignOutCompleted = "SignOutCompleted";
            public const string SessionRefreshCompleted = "SessionRefreshCompleted";
            public const string SessionRefreshPostProcessingCompleted = "SessionRefreshPostProcessingCompleted";
            public const string ProfileUpdateCompleted = "ProfileUpdateCompleted";
            public const string AvatarUploadCompleted = "AvatarUploadCompleted";
        }

        public static class Notifications
        {
            public const string EmailSent = "EmailNotificationSent";
        }

        public static class Onboarding
        {
            public const string Started = "OnboardingStarted";
            public const string ProfileCompleted = "OnboardingProfileCompleted";
            public const string Completed = "OnboardingCompleted";
        }
    }
}
