using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_AttachingKycSession_WithoutSessionRequest_Should
{
    [Fact]
    public void RejectSession()
    {
        var verification = KycVerificationTestData.CreatePending();

        Should.Throw<AggregateStateException>(() =>
            verification.AttachSession(new("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null)));
    }
}
