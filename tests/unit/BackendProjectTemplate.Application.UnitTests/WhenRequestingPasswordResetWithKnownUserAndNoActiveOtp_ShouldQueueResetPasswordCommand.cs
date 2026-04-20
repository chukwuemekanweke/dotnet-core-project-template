using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRequestingPasswordResetWithKnownUserAndNoActiveOtp_ShouldQueueResetPasswordCommand
{
    [Fact]
    public async Task Verify()
    {
        var context = new AuthenticationFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();

        context.CurrentActor.TenantId.Returns(tenantId);
        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.AppUserStakeholderRepository.GetByAppUserIdAsync(
                user.Id,
                Arg.Any<CancellationToken>())
            .Returns(AppUserStakeholder.Create(user.Id, stakeholderId, context.Clock.GetUtcNow()));

        var result = await context.CreateRequestPasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRequestPasswordResetCommand(user.Email),
            CancellationToken.None);

        result.Status.ShouldBe(RequestPasswordResetStatus.Success);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ResetPasswordCommand>(command =>
                command.TenantId == tenantId &&
                command.StakeholderId == stakeholderId),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
