using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRegeneratingRecoveryCodes_Should
{
    [Fact]
    public async Task ReturnNewCodesAndInvalidateSessions()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.IdentityService.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(["new-code-one", "new-code-two"]);
        context.IdentityService.UpdateSecurityStampAsync(user).Returns(IdentityResult.Success);
        var handler = new RegenerateTwoFactorRecoveryCodesHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            new TwoFactorProofVerifier(context.IdentityService),
            context.IdentityService,
            context.SessionService,
            Options.Create(new TwoFactorAuthenticationOptions()),
            context.CustomTelemetryContext,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new RegenerateTwoFactorRecoveryCodesCommand(
                TwoFactorVerificationMethod.Authenticator, "123456", actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(RegenerateTwoFactorRecoveryCodesStatus.Success);
        result.RecoveryCodes.ShouldBe(["new-code-one", "new-code-two"]);
        await context.IdentityService.Received(1).UpdateSecurityStampAsync(user);
        await context.SessionService.Received(1).RevokeAllAsync(stakeholderId, Arg.Any<CancellationToken>());
    }
}
