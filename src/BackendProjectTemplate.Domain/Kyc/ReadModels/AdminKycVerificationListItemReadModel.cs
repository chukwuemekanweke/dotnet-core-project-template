namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycVerificationListItemReadModel(
    Guid VerificationId,
    Guid TenantId,
    Guid StakeholderId,
    string StakeholderDisplayName,
    string ProviderKey,
    KycVerificationStatus Status,
    string? LatestProviderStatus,
    string? ProviderVerificationReference,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastReconciledAtUtc);
