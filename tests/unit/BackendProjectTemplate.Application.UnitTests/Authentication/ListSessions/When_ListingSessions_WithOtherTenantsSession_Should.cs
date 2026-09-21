using BackendProjectTemplate.Application.Authentication.Features.ListSessions;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.ListSessions;

public sealed class When_ListingSessions_WithOtherTenantsSession_Should
{
    [Fact]
    public async Task ExcludeOtherTenant()
    {
        var context = new AuthenticationFlowTestContext();
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var otherTenantId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(userId, stakeholderId, otherTenantId,
            Guid.CreateVersion7(), "Browser", null, null, null,
            context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(1));
        context.SessionService.ListActiveAsync(userId, Arg.Any<CancellationToken>())
            .Returns([session]);

        var result = await new ListSessionsHandler(context.SessionService).HandleAsync(
            new ListSessionsQuery(userId, stakeholderId, Guid.CreateVersion7(), session.Id), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
