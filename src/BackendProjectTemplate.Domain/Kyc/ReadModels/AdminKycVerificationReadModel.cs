namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycVerificationReadModel(
    Guid VerificationId,
    Guid TenantId,
    Guid StakeholderId,
    string ProviderKey,
    KycVerificationStatus Status,
    KycVerificationTransition LastProviderTransition,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastReconciledAtUtc);
