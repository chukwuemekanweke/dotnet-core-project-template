namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycProviderReadModel(
    string ProviderKey,
    string? ProviderApplicantReference,
    string? ProviderVerificationReference,
    string? ProviderSessionReference,
    string? LatestProviderStatus,
    DateTimeOffset? LatestProviderStatusObservedAtUtc);
