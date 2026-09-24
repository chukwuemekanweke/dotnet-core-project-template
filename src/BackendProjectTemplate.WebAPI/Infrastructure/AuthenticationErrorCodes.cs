namespace BackendProjectTemplate.WebAPI.Infrastructure;

public static class AuthenticationErrorCodes
{
    public const string InvalidGoogleCredential = "invalid_google_credential";
    public const string GoogleFlowInvalid = "google_flow_invalid";
    public const string GoogleFlowExpired = "google_flow_expired";
    public const string GoogleFlowConsumed = "google_flow_consumed";
    public const string AccountLocked = "account_locked";
    public const string EmailVerificationRequired = "email_verification_required";
    public const string RateLimited = "rate_limited";
    public const string InvalidCredentials = "invalid_credentials";
    public const string GoogleAccountAlreadyLinked = "google_account_already_linked";
}
