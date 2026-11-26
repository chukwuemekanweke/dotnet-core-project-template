using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Auditing.AdminAuditLogs.Export;

public sealed class When_ExportingAdministrativeAuditLogs_WithMatchingRecords_Should
{
    [Fact]
    public async Task ReturnACsvFileForTheSameFilterSemanticsAsTheList()
    {
        var context = new AdminAuditLogsControllerTestContext();
        var actorUserId = Guid.CreateVersion7();
        context.Repository.ListForExportAsync(Arg.Any<AdministrativeAuditFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await context.CreateController().Export(
            new ExportAdministrativeAuditLogsRequest(Search: " grace ", ActorUserId: actorUserId),
            CancellationToken.None);

        var file = result.ShouldBeOfType<FileContentResult>();
        file.ContentType.ShouldBe(AdminAuditLogsController.CsvContentType);
        file.FileDownloadName.ShouldStartWith("administrative-audit-");
        file.FileDownloadName.ShouldEndWith(".csv");
        Encoding.UTF8.GetString(file.FileContents).ShouldBe(string.Join(',', AdministrativeAuditCsvWriter.Headers) + "\r\n");
        await context.Repository.Received(1).ListForExportAsync(
            Arg.Is<AdministrativeAuditFilter>(filter => filter.Search == "grace" && filter.ActorUserId == actorUserId),
            ExportAdministrativeAuditsHandler.MaxRows + 1,
            Arg.Any<CancellationToken>());
    }
}
