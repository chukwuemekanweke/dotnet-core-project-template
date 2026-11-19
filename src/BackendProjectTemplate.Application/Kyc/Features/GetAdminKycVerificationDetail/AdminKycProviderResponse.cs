namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed record AdminKycProviderResponse(
    string ProviderKey,
    string? ProviderApplicantReference,
    string? ProviderVerificationReference,
    string? ProviderSessionReference,
    string? LatestProviderStatus,
    DateTimeOffset? LatestProviderStatusObservedAtUtc);
