namespace BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

public sealed record ListAdminKycVerificationsRequest(
    Guid? TenantId = null,
    Guid? StakeholderId = null,
    string? Status = null,
    string? ProviderKey = null,
    string? ProviderStatus = null,
    string? ProviderReference = null,
    string? Search = null,
    DateTimeOffset? StartedFromUtc = null,
    DateTimeOffset? StartedToUtc = null,
    string? Cursor = null,
    int Limit = 20);
