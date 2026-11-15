using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithElapsedSessionRequestLease_Should
{
    [Fact]
    public async Task RequestNewSessionOnSameAttempt()
    {
        var context = new StartKycVerificationTestContext();
        var abandonedAt = StartKycVerificationTestContext.Now - KycVerification.SessionProvisioningLease;
        var active = context.CreateAttempt(abandonedAt);
        active.MarkSessionRequested(abandonedAt);
        context.ReturnActiveAttempt(active);

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.Success);
        result.Verification!.VerificationId.ShouldBe(active.Id);
        active.SessionRequestedAtUtc.ShouldBe(StartKycVerificationTestContext.Now);
        active.VerificationUrl.ShouldBe("https://forms.kycaid.com/token-1");
        await context.Provider.Received(1).CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>());
        await context.VerificationRepository.DidNotReceive().AddAsync(Arg.Any<KycVerification>(), Arg.Any<CancellationToken>());
    }
}
