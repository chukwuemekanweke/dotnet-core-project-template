using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithSubmittedAttempt_Should
{
    [Fact]
    public async Task ReturnAttemptWithoutNewSession()
    {
        var context = new StartKycVerificationTestContext();
        var startedAt = StartKycVerificationTestContext.Now.AddHours(-1);
        var active = context.CreateAttempt(startedAt);
        active.MarkSessionRequested(startedAt);
        active.AttachSession(new("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
        active.ApplyProviderResult(KycVerificationTransition.InProgress, "applicant-1", startedAt.AddMinutes(5));
        context.ReturnActiveAttempt(active);

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.Success);
        result.Verification!.Status.ShouldBe(KycVerificationStatus.InProgress.ToString());
        result.Verification.VerificationUrl.ShouldBeNull();
        await context.Provider.DidNotReceive().CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>());
    }
}
