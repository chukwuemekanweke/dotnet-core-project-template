using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReconcileVerifications;

public sealed class When_ReconcilingKycVerifications_WithUncorrelatedProviderResult_Should
{
    [Fact]
    public async Task IgnoreResult()
    {
        var context = new KycReconciliationTestContext();
        var (stakeholder, verification) = context.CreateAttempt("verification-1");
        context.ReturnEligible(verification);
        context.Provider.GetVerificationAsync("verification-1", Arg.Any<CancellationToken>())
            .Returns(new KycProviderVerificationResult("verification-other", "applicant-1", "completed", true));

        var result = await context.RunAsync();

        result.ShouldBe(new(1, 0, 0));
        verification.Status.ShouldBe(KycVerificationStatus.Pending);
        verification.LastReconciledAtUtc.ShouldBe(KycReconciliationTestContext.Now);
        stakeholder.IsVerified.ShouldBeFalse();
    }
}
