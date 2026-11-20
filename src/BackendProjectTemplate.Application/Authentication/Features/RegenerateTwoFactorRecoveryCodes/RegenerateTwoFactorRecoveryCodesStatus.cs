namespace BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public enum RegenerateTwoFactorRecoveryCodesStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotEnabled = 3,
    InvalidProof = 4,
    Failed = 5
}
