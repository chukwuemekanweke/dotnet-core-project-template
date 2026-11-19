namespace BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;

public sealed record AdminKycVerificationSummaryResponse(
    Guid VerificationId,
    Guid TenantId,
    Guid StakeholderId,
    string StakeholderDisplayName,
    string ProviderKey,
    string Status,
    string? LatestProviderStatus,
    string? ProviderVerificationReference,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastReconciledAtUtc);
