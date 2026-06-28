using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenRequestingPasswordResetWithKnownUserAndNoActiveOtp_Should
{
    [Fact]
    public async Task QueueResetPasswordCommand()
    {
        var context = new AuthenticationFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var user = context.CreateUser();
        var stakeholder = Stakeholder.Create(
            user.Id,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName());

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateRequestPasswordResetHandler().HandleAsync(
            new RequestPasswordResetCommand(user.Email!, new ActorContext(null, tenantId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(RequestPasswordResetStatus.Success);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ResetPasswordCommand>(command =>
                command.TenantId == tenantId &&
                command.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}



