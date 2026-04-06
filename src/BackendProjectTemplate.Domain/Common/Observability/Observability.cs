namespace BackendProjectTemplate.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "BackendProjectTemplate";

    public const string SignUpRequestedEventName = "authentication.signup_requested";
    public const string UserCreatedEventName = "authentication.user_created";
    public const string UserCreatedProcessedEventName = "authentication.user_created_processed";
    public const string OtpConfirmedEventName = "authentication.otp_confirmed";
    public const string UserSignInSuccessfulEventName = "authentication.user_sign_in_successful";
    public const string UserSignInFailedEventName = "authentication.user_sign_in_failed";

    public const string MessageTypePropertyName = "MessageType";
    public const string MessageIdPropertyName = "MessageId";
    public const string UserIdPropertyName = "UserId";
}
