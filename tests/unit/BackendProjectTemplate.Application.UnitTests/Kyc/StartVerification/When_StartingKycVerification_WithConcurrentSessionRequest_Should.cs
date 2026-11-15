using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithConcurrentSessionRequest_Should
{
    [Fact]
    public async Task NotCallProviderAfterLosingTheRace()
    {
        var context = new StartKycVerificationTestContext();
        var expiredAt = StartKycVerificationTestContext.Now.AddMinutes(-1);
        var active = context.CreateAttempt(expiredAt);
        context.ReturnActiveAttempt(active);
        context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new KycVerificationConcurrencyException("conflict", new InvalidOperationException()));

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.ProviderUnavailable);
        context.UnitOfWork.Received(1).DiscardChanges();
        await context.Provider.DidNotReceive().CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>());
    }
}
