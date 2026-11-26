using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Auditing.AdminAuditLogs.List;

public sealed class When_ListingAdministrativeAuditLogs_WithValidFilters_Should
{
    [Fact]
    public async Task PassTrimmedFiltersAndPagingToTheJournal()
    {
        var context = new AdminAuditLogsControllerTestContext();
        var tenantId = Guid.CreateVersion7();
        var page = new AdministrativeAuditListPage([], 3, 5, 11);
        context.Repository.ListAsync(Arg.Any<AdministrativeAuditListRequest>(), Arg.Any<CancellationToken>()).Returns(page);

        var result = await context.CreateController().List(
            new ListAdministrativeAuditLogsRequest(Search: "  hopper ", Action: "users.locked", TenantId: tenantId, Page: 3, PageSize: 5),
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(page);
        await context.Repository.Received(1).ListAsync(
            Arg.Is<AdministrativeAuditListRequest>(request =>
                request.Filter.Search == "hopper" &&
                request.Filter.Action == "users.locked" &&
                request.Filter.TenantId == tenantId &&
                request.Page == 3 &&
                request.PageSize == 5),
            Arg.Any<CancellationToken>());
    }
}
