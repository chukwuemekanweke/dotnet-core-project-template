using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;

public sealed record ListAdminKycVerificationsCommand(
    Guid? TenantId,
    Guid? StakeholderId,
    KycVerificationStatus? Status,
    string? ProviderKey,
    string? ProviderStatus,
    string? ProviderReference,
    string? Search,
    DateTimeOffset? StartedFromUtc,
    DateTimeOffset? StartedToUtc,
    string? Cursor,
    int Limit);
