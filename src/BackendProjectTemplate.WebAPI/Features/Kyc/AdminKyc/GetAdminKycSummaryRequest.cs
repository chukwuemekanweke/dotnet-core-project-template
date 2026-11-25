namespace BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

public sealed record GetAdminKycSummaryRequest(
    Guid? TenantId = null,
    string? ProviderKey = null);
