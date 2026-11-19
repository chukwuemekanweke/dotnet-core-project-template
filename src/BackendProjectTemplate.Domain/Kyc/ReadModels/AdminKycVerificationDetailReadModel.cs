namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycVerificationDetailReadModel(
    AdminKycVerificationReadModel Verification,
    AdminKycStakeholderReadModel Stakeholder,
    AdminKycProviderReadModel Provider,
    IReadOnlyList<AdminKycCallbackReadModel> CallbackHistory);
