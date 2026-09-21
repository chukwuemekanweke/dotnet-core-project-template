using BackendProjectTemplate.Application.Authentication.Features.ListSessions;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.ListSessions;

public sealed class When_ListingSessions_WithStakeholderId_Should
{
    [Fact]
    public async Task ScopeLookupToThatStakeholder()
    {
        var context = new AuthenticationFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.SessionService.ListActiveAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuthenticationSession>());

        var result = await new ListSessionsHandler(context.SessionService).HandleAsync(
            new ListSessionsQuery(stakeholderId, sessionId), CancellationToken.None);

        result.ShouldBeEmpty();
        await context.SessionService.Received(1).ListActiveAsync(stakeholderId, Arg.Any<CancellationToken>());
    }
}
