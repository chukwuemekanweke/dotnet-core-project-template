using BackendProjectTemplate.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Authentication;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenTakingUnavailableChallenge_Should
{
    [Theory]
    [InlineData(TwoFactorChallengeStatus.Invalid, CompleteTwoFactorChallengeStatus.InvalidChallenge)]
    [InlineData(TwoFactorChallengeStatus.Expired, CompleteTwoFactorChallengeStatus.ExpiredChallenge)]
    [InlineData(TwoFactorChallengeStatus.Consumed, CompleteTwoFactorChallengeStatus.ConsumedChallenge)]
    [InlineData(TwoFactorChallengeStatus.Exhausted, CompleteTwoFactorChallengeStatus.ExhaustedChallenge)]
    public async Task ReturnStableStatus(
        TwoFactorChallengeStatus challengeStatus,
        CompleteTwoFactorChallengeStatus expected)
    {
        var context = new AuthenticationFlowTestContext();
        context.TwoFactorChallengeService.TakeAsync("opaque", Arg.Any<CancellationToken>())
            .Returns(new TwoFactorChallengeResult(challengeStatus));

        var result = await context.CreateCompleteTwoFactorChallengeHandler().HandleAsync(
            new CompleteTwoFactorChallengeCommand("opaque",
                TwoFactorVerificationMethod.Authenticator, "123456"),
            CancellationToken.None);

        result.Status.ShouldBe(expected);
        await context.SessionService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default);
    }
}
