namespace BackendProjectTemplate.Domain.Kyc;

public interface IKycProviderService
{
    string ProviderKey { get; }

    Task<KycSessionResult> CreateSessionAsync(KycSessionRequest request, CancellationToken cancellationToken);

    Task<KycProviderVerificationResult> GetVerificationAsync(string verificationReference, CancellationToken cancellationToken);
}

public sealed record KycSessionRequest(string ExternalApplicantId, string? ApplicantReference);

public sealed record KycSessionResult(
    string? VerificationReference,
    string SessionReference,
    Uri VerificationUrl,
    int? VerificationAttemptsLeft);

public sealed record KycProviderVerificationResult(
    string VerificationReference,
    string? ApplicantReference,
    string ProviderStatus,
    bool? Verified);
