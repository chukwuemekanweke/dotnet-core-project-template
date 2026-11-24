using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

public sealed class When_GettingSecurityActivityHistory_WithoutAuthenticatedScope_Should
{
    [Fact]
    public async Task ReturnNotAuthenticatedWithoutQueryingRepository()
    {
        var context = new SecurityActivityHistoryTestContext();

        foreach (var actorContext in new[]
        {
            new ActorContext(null, Guid.CreateVersion7(), "correlation-id", "flow-id"),
            new ActorContext(Guid.CreateVersion7(), null, "correlation-id", "flow-id")
        })
        {
            var result = await context.CreateHandler().HandleAsync(
                new GetSecurityActivityHistoryCommand(20, null, actorContext),
                CancellationToken.None);

            result.Status.ShouldBe(GetSecurityActivityHistoryStatus.NotAuthenticated);
            result.Activities.ShouldBeEmpty();
        }

        await context.Repository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderAsync(default!, default);
    }
}
