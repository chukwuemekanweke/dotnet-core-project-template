namespace BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public enum VerifyTwoFactorEnrollmentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    AlreadyEnabled = 3,
    InvalidCode = 4,
    Failed = 5
}
