using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.SetupTwoFactor;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSettingUpTwoFactor_Should
{
    [Fact]
    public async Task ReuseExistingAuthenticatorKey()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser("jane+totp@example.com");
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetAuthenticatorKeyAsync(user).Returns("SHAREDKEY");
        var handler = new SetupTwoFactorHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            context.IdentityService,
            Options.Create(new TwoFactorAuthenticationOptions { Issuer = "Example App" }));

        var result = await handler.HandleAsync(new SetupTwoFactorCommand(actorContext), CancellationToken.None);

        result.Status.ShouldBe(SetupTwoFactorStatus.Success);
        result.SharedKey.ShouldBe("SHAREDKEY");
        result.AuthenticatorUri.ShouldNotBeNull();
        result.AuthenticatorUri.ShouldContain("otpauth://totp/Example%20App:jane%2Btotp%40example.com");
        await context.IdentityService.DidNotReceiveWithAnyArgs().ResetAuthenticatorKeyAsync(default!);
    }
}
