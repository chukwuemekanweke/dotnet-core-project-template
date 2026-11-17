namespace BackendProjectTemplate.Domain.Kyc;

public interface IKycProviderService
{
    string ProviderKey { get; }

    Task<KycSessionResult> CreateSessionAsync(KycSessionRequest request, CancellationToken cancellationToken);

    Task<KycProviderVerificationResult> GetVerificationAsync(string verificationReference, CancellationToken cancellationToken);
}
