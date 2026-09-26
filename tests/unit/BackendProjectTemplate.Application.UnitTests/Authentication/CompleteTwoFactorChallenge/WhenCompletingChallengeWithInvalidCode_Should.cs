using BackendProjectTemplate.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenCompletingChallengeWithInvalidCode_Should
{
    [Theory]
    [InlineData(5, CompleteTwoFactorChallengeStatus.InvalidCode)]
    [InlineData(1, CompleteTwoFactorChallengeStatus.ExhaustedChallenge)]
    public async Task DecrementAttempts(int attempts, CompleteTwoFactorChallengeStatus expected)
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var challenge = new TwoFactorChallenge("opaque", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Password, new ActorContext(null, stakeholder.TenantId, "c", "f"),
            "127.0.0.1", "agent", context.Clock.GetUtcNow().AddMinutes(5), attempts);
        context.TwoFactorChallengeService.TakeAsync(challenge.Token, Arg.Any<CancellationToken>())
            .Returns(new TwoFactorChallengeResult(TwoFactorChallengeStatus.Success, challenge));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "000000").Returns(false);
        context.IdentityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await context.CreateCompleteTwoFactorChallengeHandler().HandleAsync(
            new CompleteTwoFactorChallengeCommand(challenge.Token,
                TwoFactorVerificationMethod.Authenticator, "000000"),
            CancellationToken.None);

        result.Status.ShouldBe(expected);
        await context.TwoFactorChallengeService.Received(1).RestoreAsync(
            Arg.Is<TwoFactorChallenge>(value => value.RemainingAttempts == attempts - 1),
            Arg.Any<CancellationToken>());
        await context.SessionService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default);
    }
}
