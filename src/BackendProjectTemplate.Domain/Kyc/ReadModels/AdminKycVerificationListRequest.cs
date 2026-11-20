namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycVerificationListRequest(
    Guid? TenantId,
    Guid? StakeholderId,
    KycVerificationStatus? Status,
    string? ProviderKey,
    string? ProviderStatus,
    string? ProviderReference,
    string? Search,
    DateTimeOffset? StartedFromUtc,
    DateTimeOffset? StartedToUtc,
    DateTimeOffset? CursorStartedAtUtc,
    Guid? CursorVerificationId,
    int Limit);
