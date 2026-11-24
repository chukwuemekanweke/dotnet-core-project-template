using BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.RemovePasskey;

public sealed class When_RemovingPasskey_WithOwnedCredential_Should
{
    [Fact]
    public async Task PublishPasskeyRemovedActivity()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var command = new RemovePasskeyCommand(
            user.Id, stakeholder.Id, stakeholder.TenantId, "credential-id", "203.0.113.10", "Mozilla/5.0");
        context.CurrentActor.ActorId.Returns(stakeholder.Id.ToString());
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.PasskeyService.RemoveAsync(user, command.CredentialId).Returns(PasskeyOperationStatus.Success);

        var result = await context.CreateRemovePasskeyHandler().HandleAsync(command, CancellationToken.None);

        result.ShouldBe(PasskeyOperationStatus.Success);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSecurityActivityOccurred>(message =>
                message.ActivityType == UserSecurityActivityTypes.PasskeyRemoved &&
                message.StakeholderId == stakeholder.Id &&
                message.IpAddress == command.IpAddress &&
                message.UserAgent == command.UserAgent),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
