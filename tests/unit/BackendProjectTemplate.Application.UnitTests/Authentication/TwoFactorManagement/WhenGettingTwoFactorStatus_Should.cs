using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.GetTwoFactorStatus;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenGettingTwoFactorStatus_Should
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 7)]
    public async Task ReturnEnabledStateAndRecoveryCodeCount(bool enabled, int expectedCount)
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
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(enabled);
        context.IdentityService.CountRecoveryCodesAsync(user).Returns(7);
        var handler = new GetTwoFactorStatusHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            context.IdentityService);

        var result = await handler.HandleAsync(new GetTwoFactorStatusQuery(actorContext), CancellationToken.None);

        result.Status.ShouldBe(GetTwoFactorStatusStatus.Success);
        result.Enabled.ShouldBe(enabled);
        result.RecoveryCodesRemaining.ShouldBe(expectedCount);
    }
}
