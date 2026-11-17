using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReconcileVerifications;

public sealed class When_ReconcilingKycVerifications_WithProviderFailure_Should
{
    [Fact]
    public async Task BackOffFailedAttemptAndContinueBatch()
    {
        var context = new KycReconciliationTestContext();
        var (_, unreachable) = context.CreateAttempt("verification-1");
        var (_, rejected) = context.CreateAttempt("verification-2");
        context.ReturnEligible(unreachable, rejected);
        context.Provider.GetVerificationAsync("verification-1", Arg.Any<CancellationToken>())
            .Returns<KycProviderVerificationResult>(_ => throw new HttpRequestException("unavailable"));
        context.Provider.GetVerificationAsync("verification-2", Arg.Any<CancellationToken>())
            .Returns(new KycProviderVerificationResult("verification-2", "applicant-2", KycVerificationTransition.Rejected));

        var result = await context.RunAsync();

        result.ShouldBe(new(2, 1, 1));
        unreachable.Status.ShouldBe(KycVerificationStatus.Pending);
        unreachable.LastReconciledAtUtc.ShouldBe(KycReconciliationTestContext.Now);
        rejected.Status.ShouldBe(KycVerificationStatus.Rejected);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
