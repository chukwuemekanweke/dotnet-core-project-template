using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Infrastructure.Kyc.Kycaid;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class When_NormalizingKycaidVerificationResult_WithProviderOutcome_Should
{
    [Theory]
    [InlineData("pending", null, KycVerificationTransition.InProgress)]
    [InlineData("completed", true, KycVerificationTransition.Verified)]
    [InlineData("completed", false, KycVerificationTransition.Rejected)]
    [InlineData("completed", null, KycVerificationTransition.NoChange)]
    [InlineData("unexpected", true, KycVerificationTransition.NoChange)]
    public void ReturnProviderIndependentTransition(
        string providerStatus,
        bool? verified,
        KycVerificationTransition expectedTransition)
    {
        var normalizer = new KycaidVerificationResultNormalizer();

        normalizer.Normalize(providerStatus, verified).ShouldBe(expectedTransition);
    }
}
