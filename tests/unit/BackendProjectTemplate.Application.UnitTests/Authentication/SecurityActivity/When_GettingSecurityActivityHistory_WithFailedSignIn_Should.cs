using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

public sealed class When_GettingSecurityActivityHistory_WithFailedSignIn_Should
{
    [Fact]
    public async Task ReturnActivityTypeAndFailureReason()
    {
        var context = new SecurityActivityHistoryTestContext();
        var failureReason = "invalid_two_factor_code";
        context.Repository.GetByStakeholderAsync(
                Arg.Any<SecurityActivityHistoryCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityHistoryCursorPage(
                [new SecurityActivityHistoryReadModel(
                    Guid.CreateVersion7(),
                    SecurityActivityType.FailedSignIn,
                    DateTimeOffset.UtcNow,
                    "203.0.113.10",
                    "Desktop",
                    "Windows",
                    "Chrome",
                    "Lagos",
                    "Lagos",
                    "Nigeria",
                    failureReason)],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetSecurityActivityHistoryCommand(
                20,
                null,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.Status.ShouldBe(GetSecurityActivityHistoryStatus.Success);
        result.Activities.Count.ShouldBe(1);
        result.Activities[0].ActivityType.ShouldBe(nameof(SecurityActivityType.FailedSignIn));
        result.Activities[0].FailureReason.ShouldBe(failureReason);
    }
}
