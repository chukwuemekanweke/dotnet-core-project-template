using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_DeletingSession_WithAnotherTenantsSession_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new AuthenticationControllerTestContext();
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(userId, stakeholderId, Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Browser", null, null, null,
            context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(1));
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var controller = context.CreateSessionsController(
            AuthenticationControllerTestContext.CreateSessionPrincipal(userId, stakeholderId, Guid.CreateVersion7()));

        var result = await controller.DeleteSession(session.Id, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
        await context.SessionService.DidNotReceive().RevokeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
