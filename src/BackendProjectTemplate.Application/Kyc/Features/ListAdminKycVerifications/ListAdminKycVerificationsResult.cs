namespace BackendProjectTemplate.Application.Kyc.Features.ListAdminKycVerifications;

public sealed record ListAdminKycVerificationsResult(
    IReadOnlyList<AdminKycVerificationSummaryResponse> Items,
    string? NextCursor);
