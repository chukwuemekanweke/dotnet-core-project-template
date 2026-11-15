using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReconcileVerifications;

public sealed class When_ReconcilingKycVerifications_WithNoEligibleAttempts_Should
{
    [Fact]
    public async Task NotCallProviderOrSave()
    {
        var context = new KycReconciliationTestContext();
        context.ReturnEligible();

        var result = await context.RunAsync();

        result.ShouldBe(new(0, 0, 0));
        await context.Provider.DidNotReceive().GetVerificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
