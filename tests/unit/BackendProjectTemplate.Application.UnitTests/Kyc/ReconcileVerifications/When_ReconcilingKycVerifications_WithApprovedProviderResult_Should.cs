using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReconcileVerifications;

public sealed class When_ReconcilingKycVerifications_WithApprovedProviderResult_Should
{
    [Fact]
    public async Task VerifyAttemptAndStakeholder()
    {
        var context = new KycReconciliationTestContext();
        var (stakeholder, verification) = context.CreateAttempt("verification-1");
        context.ReturnEligible(verification);
        context.Provider.GetVerificationAsync("verification-1", Arg.Any<CancellationToken>())
            .Returns(new KycProviderVerificationResult("verification-1", "applicant-1", "completed", true));

        var result = await context.RunAsync();

        result.ShouldBe(new(1, 1, 0));
        verification.Status.ShouldBe(KycVerificationStatus.Verified);
        verification.LastReconciledAtUtc.ShouldBe(KycReconciliationTestContext.Now);
        stakeholder.IsVerified.ShouldBeTrue();
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
