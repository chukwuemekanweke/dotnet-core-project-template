using BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.RequestEmailConfirmationOtp;

public sealed class When_RequestingEmailConfirmationOtp_WithoutActiveOtp_Should
{
    [Fact]
    public async Task QueueNewCodeAndReturnNewRetryTime()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var user = context.CreateUser(email);
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName());
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.TwoFactorOtpService.GetActiveOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns((TwoFactorOtp?)null);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var command = new RequestEmailConfirmationOtpCommand(
            email,
            AuthenticationFlowTestContext.CreateSignUpCommand(email: email).ActorContext);

        var result = await context.CreateRequestEmailConfirmationOtpHandler().HandleAsync(
            command,
            CancellationToken.None);

        result.Status.ShouldBe(RequestEmailConfirmationOtpStatus.Accepted);
        result.RetryAtUtc.ShouldBe(context.Clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<SendEmailConfirmationOtpCommand>(queued =>
                queued.StakeholderId == stakeholder.Id &&
                queued.TenantId == command.ActorContext.TenantId),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
