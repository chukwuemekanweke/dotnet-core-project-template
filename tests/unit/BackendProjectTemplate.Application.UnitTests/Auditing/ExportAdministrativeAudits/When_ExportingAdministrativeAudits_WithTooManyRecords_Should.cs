using BackendProjectTemplate.Application.Auditing.Features.ExportAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Auditing.ExportAdministrativeAudits;

public sealed class When_ExportingAdministrativeAudits_WithTooManyRecords_Should
{
    [Fact]
    public async Task RefuseInsteadOfTruncating()
    {
        var repository = Substitute.For<IAdministrativeAuditReadModelRepository>();
        var filter = new AdministrativeAuditFilter("x", null, null, null, null, null, null, null, null);
        var row = new AdministrativeAuditExportRow(
            Guid.CreateVersion7(), DateTimeOffset.UnixEpoch, Guid.CreateVersion7(), "A", "users.locked", "User",
            Guid.CreateVersion7(), null, null, null, "c");
        repository.ListForExportAsync(filter, ExportAdministrativeAuditsHandler.MaxRows + 1, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Repeat(row, ExportAdministrativeAuditsHandler.MaxRows + 1).ToList());

        var result = await new ExportAdministrativeAuditsHandler(repository)
            .HandleAsync(new ExportAdministrativeAuditsQuery(filter), CancellationToken.None);

        result.Status.ShouldBe(ExportAdministrativeAuditsStatus.TooManyRecords);
        result.Csv.ShouldBeNull();
        result.Error.ShouldNotBeNull().ShouldContain(ExportAdministrativeAuditsHandler.MaxRows.ToString());
    }
}
