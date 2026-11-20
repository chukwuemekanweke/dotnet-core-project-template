namespace BackendProjectTemplate.Application.Kyc.Features.StartVerification;

public enum StartKycVerificationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    AlreadyVerified = 4,
    IncompleteProfile = 5,
    ProviderUnavailable = 6
}
