using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

public sealed class When_GettingSecurityActivityHistory_WithFinalPage_Should
{
    [Fact]
    public async Task ReturnNullNextCursor()
    {
        var context = new SecurityActivityHistoryTestContext();
        context.Repository.GetByStakeholderAsync(
                Arg.Any<SecurityActivityHistoryCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityHistoryCursorPage(
                [new SecurityActivityHistoryReadModel(
                    Guid.CreateVersion7(), SecurityActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    "203.0.113.10", null, null, null, "Lagos", "Lagos", "Nigeria", null)],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetSecurityActivityHistoryCommand(
                20,
                null,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.NextCursor.ShouldBeNull();
        result.Activities[0].Country.ShouldBe("Nigeria");
    }
}
