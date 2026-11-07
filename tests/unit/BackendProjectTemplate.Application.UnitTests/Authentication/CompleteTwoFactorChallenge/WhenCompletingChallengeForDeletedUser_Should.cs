using BackendProjectTemplate.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenCompletingChallengeForDeletedUser_Should
{
    [Fact]
    public async Task RejectBeforeVerifyingCodeOrCreatingSession()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        user.SetDeletedAudit(context.Clock.GetUtcNow(), "test");
        var actorContext = new ActorContext(null, Guid.CreateVersion7(), "correlation", "flow");
        var challenge = new TwoFactorChallenge(
            "opaque",
            user.Id,
            Guid.CreateVersion7(),
            actorContext.TenantId!.Value,
            AuthenticationMethod.Passkey,
            actorContext,
            "127.0.0.1",
            "agent",
            context.Clock.GetUtcNow().AddMinutes(5),
            5);
        context.TwoFactorChallengeService.TakeAsync(challenge.Token, Arg.Any<CancellationToken>())
            .Returns(new TwoFactorChallengeResult(TwoFactorChallengeStatus.Success, challenge));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);

        var result = await context.CreateCompleteTwoFactorChallengeHandler().HandleAsync(
            new CompleteTwoFactorChallengeCommand(
                challenge.Token,
                TwoFactorVerificationMethod.Authenticator,
                "123456"),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteTwoFactorChallengeStatus.InvalidChallenge);
        await context.IdentityService.DidNotReceiveWithAnyArgs()
            .VerifyAuthenticatorTokenAsync(default!, default!);
        await context.SessionService.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default!, default, default);
    }
}
