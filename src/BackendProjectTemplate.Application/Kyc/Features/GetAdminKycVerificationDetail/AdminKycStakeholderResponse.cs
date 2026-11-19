namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed record AdminKycStakeholderResponse(
    Guid StakeholderId,
    string DisplayName,
    bool IsVerified);
