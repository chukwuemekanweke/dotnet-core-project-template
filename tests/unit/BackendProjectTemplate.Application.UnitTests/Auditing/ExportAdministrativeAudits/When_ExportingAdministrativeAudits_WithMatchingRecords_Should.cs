using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Auditing.ExportAdministrativeAudits;

public sealed class When_ExportingAdministrativeAudits_WithMatchingRecords_Should
{
    [Fact]
    public async Task WriteEscapedCsvProtectedAgainstFormulaInjection()
    {
        var repository = Substitute.For<IAdministrativeAuditReadModelRepository>();
        var filter = new AdministrativeAuditFilter(null, null, null, null, null, null, null, null, null);
        var row = new AdministrativeAuditExportRow(
            Guid.Parse("019a0000-0000-7000-8000-000000000001"),
            new DateTimeOffset(2026, 11, 24, 16, 2, 0, TimeSpan.Zero),
            Guid.Parse("019a0000-0000-7000-8000-000000000002"),
            "=cmd|' /C calc'!A0",
            AdministrativeAuditActions.Users.Locked,
            AdministrativeAuditTargetTypes.User,
            Guid.Parse("019a0000-0000-7000-8000-000000000003"),
            "Bakare, \"Tunde\"",
            "+line one\nline two",
            null,
            "corr-1");
        repository.ListForExportAsync(filter, ExportAdministrativeAuditsHandler.MaxRows + 1, Arg.Any<CancellationToken>())
            .Returns([row]);

        var result = await new ExportAdministrativeAuditsHandler(repository)
            .HandleAsync(new ExportAdministrativeAuditsQuery(filter), CancellationToken.None);

        result.Status.ShouldBe(ExportAdministrativeAuditsStatus.Exported);
        result.RowCount.ShouldBe(1);
        result.Csv.ShouldBe(
            "id,occurredAtUtc,actorUserId,actorDisplayName,action,targetType,targetId,targetDisplayName,reason,targetTenantId,correlationId\r\n" +
            "019a0000-0000-7000-8000-000000000001,2026-11-24T16:02:00.0000000Z,019a0000-0000-7000-8000-000000000002," +
            "'=cmd|' /C calc'!A0,users.locked,User,019a0000-0000-7000-8000-000000000003," +
            "\"Bakare, \"\"Tunde\"\"\",\"'+line one\nline two\",,corr-1\r\n");
        AdministrativeAuditCsvWriter.EscapeCell("-1+1").ShouldBe("'-1+1");
        AdministrativeAuditCsvWriter.EscapeCell("@SUM(A1)").ShouldBe("'@SUM(A1)");
        AdministrativeAuditCsvWriter.EscapeCell("\tTab").ShouldBe("'\tTab");
        AdministrativeAuditCsvWriter.EscapeCell("plain").ShouldBe("plain");
        AdministrativeAuditCsvWriter.EscapeCell(null).ShouldBe(string.Empty);
    }
}
