namespace BackendProjectTemplate.Domain.Kyc;

public interface IKycVerificationResultNormalizer
{
    KycVerificationTransition Normalize(string? providerStatus, bool? verified);
}
