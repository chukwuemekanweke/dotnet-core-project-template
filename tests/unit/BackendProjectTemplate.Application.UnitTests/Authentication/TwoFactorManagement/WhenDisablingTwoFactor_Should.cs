using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.DisableTwoFactor;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenDisablingTwoFactor_Should
{
    [Fact]
    public async Task ResetKeyAndRevokeOtherSessions()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.IdentityService.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);
        context.IdentityService.ResetAuthenticatorKeyAsync(user).Returns(IdentityResult.Success);
        var handler = new DisableTwoFactorHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            new TwoFactorProofVerifier(context.IdentityService),
            context.IdentityService,
            context.SessionService,
            context.CustomTelemetryContext,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new DisableTwoFactorCommand(
                TwoFactorVerificationMethod.Authenticator, "123456", sessionId, actorContext),
            CancellationToken.None);

        result.ShouldBe(DisableTwoFactorResult.Success);
        await context.IdentityService.Received(1).ResetAuthenticatorKeyAsync(user);
        await context.IdentityService.DidNotReceiveWithAnyArgs().UpdateSecurityStampAsync(default!);
        await context.SessionService.Received(1)
            .RevokeOthersAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>());
    }
}
