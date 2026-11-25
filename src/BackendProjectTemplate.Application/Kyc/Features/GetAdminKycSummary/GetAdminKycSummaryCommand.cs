namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;

public sealed record GetAdminKycSummaryCommand(Guid? TenantId, string? ProviderKey);
