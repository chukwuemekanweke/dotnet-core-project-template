using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_MarkingKycFormUrlExpired_WithSubmittedAttempt_Should
{
    [Fact]
    public void KeepAttemptCorrelatableWithPendingDecision()
    {
        var verification = KycVerificationTestData.CreateWithSession();
        var submittedAt = KycVerificationTestData.StartedAt.AddMinutes(1);
        var expiredAt = submittedAt.AddMinutes(1);
        verification.ApplyProviderResult("pending", null, "applicant-1", submittedAt);

        verification.MarkFormUrlExpired(expiredAt);

        verification.Status.ShouldBe(KycVerificationStatus.InProgress);
        verification.CompletedAtUtc.ShouldBeNull();
        verification.ProviderVerificationReference.ShouldBe("verification-1");
        verification.ProviderSessionReference.ShouldBe("token-1");
        verification.VerificationUrl.ShouldBeNull();
        verification.FormUrlExpiresAtUtc.ShouldBe(expiredAt);
    }
}
