namespace BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed record VerifyTwoFactorEnrollmentResult(
    VerifyTwoFactorEnrollmentStatus Status,
    IReadOnlyList<string>? RecoveryCodes = null);
