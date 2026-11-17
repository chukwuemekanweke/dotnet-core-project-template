using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_ApplyingKycProviderResult_WithInactiveAttempt_Should
{
    [Theory]
    [InlineData(KycVerificationStatus.Failed)]
    [InlineData(KycVerificationStatus.Expired)]
    public void IgnoreApprovedResult(KycVerificationStatus terminalStatus)
    {
        var verification = KycVerificationTestData.CreateWithSession();
        var closedAt = KycVerificationTestData.StartedAt.AddMinutes(1);
        if (terminalStatus == KycVerificationStatus.Failed)
        {
            verification.MarkFailed(closedAt);
        }
        else
        {
            verification.MarkExpired(closedAt);
        }

        verification.ApplyProviderResult(KycVerificationTransition.Verified, "applicant-1", closedAt.AddMinutes(1));

        verification.Status.ShouldBe(terminalStatus);
        verification.CompletedAtUtc.ShouldBe(closedAt);
    }
}
