using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SecurityActivity;

public sealed class When_GettingSecurityActivityHistory_WithValidActor_Should
{
    [Fact]
    public async Task ReturnMappedHistoryScopedToTenantAndStakeholder()
    {
        var context = new SecurityActivityHistoryTestContext();
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var occurredAtUtc = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        SecurityActivityHistoryCursorRequest? capturedRequest = null;
        context.Repository.GetByStakeholderAsync(
                Arg.Do<SecurityActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityHistoryCursorPage(
                [new SecurityActivityHistoryReadModel(
                    Guid.CreateVersion7(),
                    SecurityActivityType.InitialLogin,
                    occurredAtUtc,
                    "203.0.113.10",
                    "Desktop",
                    "Windows",
                    "Chrome",
                    null,
                    null,
                    null,
                    null)],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetSecurityActivityHistoryCommand(
                20,
                null,
                new ActorContext(stakeholderId, tenantId, "correlation-id", "flow-id")),
            CancellationToken.None);

        result.Status.ShouldBe(GetSecurityActivityHistoryStatus.Success);
        result.Activities.Count.ShouldBe(1);
        result.Activities[0].ActivityType.ShouldBe(nameof(SecurityActivityType.InitialLogin));
        result.Activities[0].OccurredAtUtc.ShouldBe(occurredAtUtc);
        result.Activities[0].IpAddress.ShouldBe("203.0.113.10");
        result.Activities[0].City.ShouldBeNull();
        result.NextCursor.ShouldBeNull();
        capturedRequest.ShouldNotBeNull();
        capturedRequest.TenantId.ShouldBe(tenantId);
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
    }
}
