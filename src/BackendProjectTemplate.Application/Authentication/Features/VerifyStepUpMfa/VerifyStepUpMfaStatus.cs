namespace BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;

public enum VerifyStepUpMfaStatus
{
    Success = 1,
    NotAuthenticated = 2,
    EnrollmentRequired = 3,
    InvalidMethod = 4,
    InvalidOperation = 5,
    InvalidCode = 6,
    CodeReplayed = 7,
    AccountLocked = 8,
    Failed = 9
}
