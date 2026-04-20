using BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;
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
        var stakeholder = Stakeholder.Create(
            user.Id,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName(),
            context.Clock.GetUtcNow());

        context.CurrentActor.TenantId.Returns(tenantId);
        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateRequestPasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRequestPasswordResetCommand(user.Email),
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
