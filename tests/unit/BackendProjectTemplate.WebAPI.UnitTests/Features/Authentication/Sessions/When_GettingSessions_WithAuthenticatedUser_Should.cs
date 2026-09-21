using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_GettingSessions_WithAuthenticatedUser_Should
{
    [Fact]
    public async Task ReturnOwnedActiveSessions()
    {
        var context = new AuthenticationControllerTestContext();
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.SessionService.ListActiveAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuthenticationSession>());
        var controller = context.CreateSessionsController(
            AuthenticationControllerTestContext.CreateSessionPrincipal(userId, stakeholderId, sessionId));

        var result = await controller.GetSessions(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
        await context.SessionService.Received(1).ListActiveAsync(userId, Arg.Any<CancellationToken>());
    }
}
