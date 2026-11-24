using BackendProjectTemplate.Application.Authentication.Features.CompletePasskeyRegistration;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.CompletePasskeyRegistration;

public sealed class When_CompletingPasskeyRegistration_WithExpiredFlow_Should
{
    [Fact]
    public async Task NotPublishSecurityActivity()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var command = new CompletePasskeyRegistrationCommand(
            user.Id, stakeholder.Id, stakeholder.TenantId, "flow-token", "{}", "203.0.113.10", "Mozilla/5.0");
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.PasskeyService.CompleteRegistrationAsync(
                user, stakeholder.Id, stakeholder.TenantId, command.FlowToken, command.CredentialJson, Arg.Any<CancellationToken>())
            .Returns(new PasskeyRegistrationResult(PasskeyOperationStatus.ExpiredFlow));

        var result = await context.CreateCompletePasskeyRegistrationHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(PasskeyOperationStatus.ExpiredFlow);
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<UserSecurityActivityOccurred>(),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
