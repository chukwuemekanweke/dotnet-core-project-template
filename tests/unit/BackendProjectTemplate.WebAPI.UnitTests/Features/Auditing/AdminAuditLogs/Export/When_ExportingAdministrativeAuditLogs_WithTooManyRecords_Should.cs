using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Auditing.AdminAuditLogs.Export;

public sealed class When_ExportingAdministrativeAuditLogs_WithTooManyRecords_Should
{
    [Fact]
    public async Task ReturnUnprocessableEntityInsteadOfATruncatedFile()
    {
        var context = new AdminAuditLogsControllerTestContext();
        var row = new AdministrativeAuditExportRow(
            Guid.CreateVersion7(), DateTimeOffset.UnixEpoch, Guid.CreateVersion7(), "A", "users.locked", "User",
            Guid.CreateVersion7(), null, null, null, "c");
        context.Repository.ListForExportAsync(Arg.Any<AdministrativeAuditFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Repeat(row, ExportAdministrativeAuditsHandler.MaxRows + 1).ToList());

        var result = await context.CreateController().Export(new ExportAdministrativeAuditLogsRequest(), CancellationToken.None);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }
}
