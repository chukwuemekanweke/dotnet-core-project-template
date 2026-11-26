using BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Auditing.AdminAuditLogs.List;

public sealed class When_ListingAdministrativeAuditLogs_WithInvalidFilters_Should
{
    [Fact]
    public async Task ReturnValidationProblemsWithoutQueryingTheJournal()
    {
        var context = new AdminAuditLogsControllerTestContext();
        var request = new ListAdministrativeAuditLogsRequest(
            Search: "x",
            Action: "users.lockd",
            ActorUserId: Guid.Empty,
            TargetType: "Invoice",
            TenantId: Guid.Empty,
            CorrelationId: new string('c', 200),
            FromUtc: new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero),
            ToUtc: new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            Page: 0,
            PageSize: 101);

        var result = await context.CreateController().List(request, CancellationToken.None);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ValidationProblemDetails>();
        problem.Errors.Keys.ShouldBe(
            ["Search", "Action", "ActorUserId", "TargetType", "TenantId", "CorrelationId", "ToUtc", "Page", "PageSize"],
            ignoreOrder: true);
        await context.Repository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}
