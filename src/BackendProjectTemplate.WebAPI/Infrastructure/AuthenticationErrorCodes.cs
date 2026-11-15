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
    public const string InvalidTwoFactorCode = "invalid_two_factor_code";
    public const string TwoFactorChallengeInvalid = "two_factor_challenge_invalid";
    public const string TwoFactorChallengeExpired = "two_factor_challenge_expired";
    public const string TwoFactorChallengeConsumed = "two_factor_challenge_consumed";
    public const string TwoFactorChallengeExhausted = "two_factor_challenge_exhausted";
    public const string MfaEnrollmentRequired = "mfa_enrollment_required";
    public const string MfaVerificationRequired = "mfa_verification_required";
    public const string MfaVerificationFailed = "mfa_verification_failed";
    public const string MfaProofExpired = "mfa_proof_expired";
    public const string MfaProofInvalid = "mfa_proof_invalid";
    public const string MfaProofConsumed = "mfa_proof_consumed";
    public const string MfaOperationMismatch = "mfa_operation_mismatch";
    public const string MfaVerificationRateLimited = "mfa_verification_rate_limited";
}
