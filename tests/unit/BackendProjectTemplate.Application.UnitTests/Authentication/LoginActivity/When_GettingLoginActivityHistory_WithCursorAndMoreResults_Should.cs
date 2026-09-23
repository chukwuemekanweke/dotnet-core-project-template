using BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;
using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.LoginActivity;

public sealed class When_GettingLoginActivityHistory_WithCursorAndMoreResults_Should
{
    [Fact]
    public async Task PassCursorAndReturnNextCursorFromLastActivity()
    {
        var context = new LoginActivityHistoryTestContext();
        var cursorId = Guid.CreateVersion7();
        var cursorTime = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var newestId = Guid.CreateVersion7();
        var oldestId = Guid.CreateVersion7();
        var newestTime = cursorTime.AddMinutes(-1);
        var oldestTime = cursorTime.AddMinutes(-2);
        LoginActivityHistoryCursorRequest? capturedRequest = null;
        context.Repository.GetByStakeholderAsync(
                Arg.Do<LoginActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [
                    new LoginActivityHistoryReadModel(newestId, LoginActivityType.TokenRefresh, newestTime, null, null, null, null, null, null),
                    new LoginActivityHistoryReadModel(oldestId, LoginActivityType.InitialLogin, oldestTime, null, null, null, null, null, null)
                ],
                true));

        var result = await context.CreateHandler().HandleAsync(
            new GetLoginActivityHistoryCommand(
                2,
                CursorPagination.Encode(cursorTime, cursorId),
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.CursorOccurredAtUtc.ShouldBe(cursorTime);
        capturedRequest.CursorLoginActivityId.ShouldBe(cursorId);
        result.Activities.Select(activity => activity.Id).ShouldBe([newestId, oldestId]);
        result.NextCursor.ShouldBe(CursorPagination.Encode(oldestTime, oldestId));
    }
}
