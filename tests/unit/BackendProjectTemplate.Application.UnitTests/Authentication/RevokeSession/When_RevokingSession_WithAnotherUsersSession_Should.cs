using BackendProjectTemplate.Application.Authentication.Features.RevokeSession;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.RevokeSession;

public sealed class When_RevokingSession_WithAnotherUsersSession_Should
{
    [Fact]
    public async Task RejectRevocation()
    {
        var context = new AuthenticationFlowTestContext();
        var session = AuthenticationSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Browser", null, null, null,
            context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(1));
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await new RevokeSessionHandler(context.SessionService, context.UnitOfWork).HandleAsync(
            new RevokeSessionCommand(session.Id, Guid.CreateVersion7(), session.StakeholderId,
                session.TenantId), CancellationToken.None);

        result.ShouldBeFalse();
        await context.SessionService.DidNotReceive().RevokeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
