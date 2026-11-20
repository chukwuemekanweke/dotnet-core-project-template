namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycStakeholderReadModel(
    Guid StakeholderId,
    string DisplayName,
    bool IsVerified);
