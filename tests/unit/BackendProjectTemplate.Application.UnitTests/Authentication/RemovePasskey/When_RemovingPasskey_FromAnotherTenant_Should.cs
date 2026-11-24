using BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.RemovePasskey;

public sealed class When_RemovingPasskey_FromAnotherTenant_Should
{
    [Fact]
    public async Task ReturnNotFoundWithoutRemovingOrPublishing()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var command = new RemovePasskeyCommand(
            user.Id, stakeholder.Id, Guid.CreateVersion7(), "credential-id", "203.0.113.10", "Mozilla/5.0");
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await context.CreateRemovePasskeyHandler().HandleAsync(command, CancellationToken.None);

        result.ShouldBe(PasskeyOperationStatus.NotFound);
        await context.PasskeyService.DidNotReceive().RemoveAsync(Arg.Any<AppUser>(), Arg.Any<string>());
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<UserSecurityActivityOccurred>(),
            Arg.Any<CancellationToken>());
    }
}
