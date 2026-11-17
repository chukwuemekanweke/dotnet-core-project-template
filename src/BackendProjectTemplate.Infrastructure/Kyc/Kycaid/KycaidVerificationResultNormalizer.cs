using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Infrastructure.Kyc.Kycaid;

internal sealed class KycaidVerificationResultNormalizer : IKycVerificationResultNormalizer
{
    public KycVerificationTransition Normalize(string? providerStatus, bool? verified)
    {
        var normalizedStatus = providerStatus?.Trim().ToLowerInvariant();
        if (normalizedStatus == "completed" && verified.HasValue)
        {
            return verified.Value ? KycVerificationTransition.Verified : KycVerificationTransition.Rejected;
        }

        return normalizedStatus == "pending"
            ? KycVerificationTransition.InProgress
            : KycVerificationTransition.NoChange;
    }
}
