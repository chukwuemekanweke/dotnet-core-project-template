namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycVerificationListPage(
    IReadOnlyList<AdminKycVerificationListItemReadModel> Items,
    bool HasMore);
