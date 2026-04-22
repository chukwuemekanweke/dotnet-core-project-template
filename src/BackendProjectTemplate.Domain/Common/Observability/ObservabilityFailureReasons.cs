namespace BackendProjectTemplate.Domain.Common.Observability;

public static class ObservabilityFailureReasons
{
    public const string AlreadyConfirmed = "already_confirmed";
    public const string DuplicateEmail = "duplicate_email";
    public const string DuplicateGoogleAccount = "duplicate_google_account";
    public const string InvalidFile = "invalid_file";
    public const string InvalidGoogleToken = "invalid_google_token";
    public const string InvalidOtp = "invalid_otp";
    public const string NotAuthenticated = "not_authenticated";
    public const string StakeholderNotFound = "stakeholder_not_found";
    public const string UserNotFound = "user_not_found";
    public const string ValidationFailed = "validation_failed";
}
