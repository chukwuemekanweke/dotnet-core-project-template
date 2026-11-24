using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

public sealed class When_GettingSecurityActivityHistory_WithCursorAndMoreResults_Should
{
    [Fact]
    public async Task PassCursorAndReturnNextCursorFromLastActivity()
    {
        var context = new SecurityActivityHistoryTestContext();
        var cursorId = Guid.CreateVersion7();
        var cursorTime = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var newestId = Guid.CreateVersion7();
        var oldestId = Guid.CreateVersion7();
        var newestTime = cursorTime.AddMinutes(-1);
        var oldestTime = cursorTime.AddMinutes(-2);
        SecurityActivityHistoryCursorRequest? capturedRequest = null;
        context.Repository.GetByStakeholderAsync(
                Arg.Do<SecurityActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityHistoryCursorPage(
                [
                    new SecurityActivityHistoryReadModel(newestId, SecurityActivityType.InitialLogin, newestTime, "203.0.113.10", null, null, null, null, null, null, null),
                    new SecurityActivityHistoryReadModel(oldestId, SecurityActivityType.InitialLogin, oldestTime, "203.0.113.11", null, null, null, null, null, null, null)
                ],
                true));

        var result = await context.CreateHandler().HandleAsync(
            new GetSecurityActivityHistoryCommand(
                2,
                CursorPagination.Encode(cursorTime, cursorId),
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.CursorOccurredAtUtc.ShouldBe(cursorTime);
        capturedRequest.CursorSecurityActivityId.ShouldBe(cursorId);
        result.Activities.Select(activity => activity.Id).ShouldBe([newestId, oldestId]);
        result.NextCursor.ShouldBe(CursorPagination.Encode(oldestTime, oldestId));
    }
}
