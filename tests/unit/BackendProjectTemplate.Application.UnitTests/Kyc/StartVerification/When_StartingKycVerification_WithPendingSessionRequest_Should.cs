using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithPendingSessionRequest_Should
{
    [Fact]
    public async Task NotRequestAnotherProviderSession()
    {
        var context = new StartKycVerificationTestContext();
        var active = context.CreateAttempt(StartKycVerificationTestContext.Now.AddMinutes(-2));
        active.MarkSessionRequested(StartKycVerificationTestContext.Now.AddMinutes(-1));
        context.ReturnActiveAttempt(active);

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.ProviderUnavailable);
        await context.Provider.DidNotReceive().CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
