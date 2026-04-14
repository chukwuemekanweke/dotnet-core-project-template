namespace BackendProjectTemplate.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "BackendProjectTemplate";

    public const string MessageTypePropertyName = "MessageType";
    public const string MessageIdPropertyName = "MessageId";
    public const string UserIdPropertyName = "UserId";
    public const string StakeholderIdPropertyName = "StakeholderId";

    public static class EventNames
    {
        public static class Authentication
        {
            public const string SignUpRequested = "authentication.signup_requested";
            public const string UserCreated = "authentication.user_created";
            public const string UserCreatedProcessed = "authentication.user_created_processed";
            public const string OtpConfirmed = "authentication.otp_confirmed";
            public const string UserSignInSuccessful = "authentication.user_sign_in_successful";
            public const string UserSignInFailed = "authentication.user_sign_in_failed";
        }

        public static class Notifications
        {
            public const string EmailSent = "notifications.email_sent";
        }
    }
}
