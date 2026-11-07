using BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningInWithPasskeyFromAnotherTenant_Should
{
    [Fact]
    public async Task RejectBeforeCreatingSession()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var requestedTenantId = Guid.CreateVersion7();
        var command = new CompletePasskeySignInCommand(
            requestedTenantId, "flow-token", "{}", "127.0.0.1", "browser",
            new ActorContext(null, requestedTenantId, "correlation", "flow"));
        context.PasskeyService.VerifyAuthenticationAsync(
                requestedTenantId, command.FlowToken, command.CredentialJson, Arg.Any<CancellationToken>())
            .Returns(new PasskeyAuthenticationResult(PasskeyOperationStatus.Success, user, context.CreatePasskeyInfo()));
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await context.CreateCompletePasskeySignInHandler()
            .HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(CompletePasskeySignInStatus.TenantMismatch);
        await context.PasskeyService.DidNotReceiveWithAnyArgs()
            .UpdateAuthenticationStateAsync(default!, default!);
        await context.SessionService.DidNotReceiveWithAnyArgs()
            .CreateAsync(default!, default!, default!, default, default);
    }
}
