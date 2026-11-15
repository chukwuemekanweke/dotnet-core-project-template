using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_MarkingKycFormUrlExpired_WithUnsubmittedAttempt_Should
{
    [Fact]
    public void KeepAttemptOpenAndAllowNewSession()
    {
        var verification = KycVerificationTestData.CreateWithSession();
        var expiredAt = KycVerificationTestData.StartedAt.AddMinutes(1);

        verification.MarkFormUrlExpired(expiredAt);

        verification.Status.ShouldBe(KycVerificationStatus.Pending);
        verification.CompletedAtUtc.ShouldBeNull();
        verification.VerificationUrl.ShouldBeNull();
        verification.ProviderSessionReference.ShouldBeNull();
        verification.ProviderVerificationReference.ShouldBeNull();
        verification.HasPendingSessionRequest(expiredAt).ShouldBeFalse();
        Should.NotThrow(() => verification.MarkSessionRequested(expiredAt));
    }
}
