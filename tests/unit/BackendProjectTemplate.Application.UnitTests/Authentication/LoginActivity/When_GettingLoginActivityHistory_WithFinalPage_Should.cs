using BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.LoginActivity;

public sealed class When_GettingLoginActivityHistory_WithFinalPage_Should
{
    [Fact]
    public async Task ReturnNullNextCursor()
    {
        var context = new LoginActivityHistoryTestContext();
        context.Repository.GetByStakeholderAsync(
                Arg.Any<LoginActivityHistoryCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [new LoginActivityHistoryReadModel(
                    Guid.CreateVersion7(), LoginActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    null, null, null, "Lagos", "Lagos", "Nigeria")],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetLoginActivityHistoryCommand(
                20,
                null,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.NextCursor.ShouldBeNull();
        result.Activities[0].Country.ShouldBe("Nigeria");
    }
}
