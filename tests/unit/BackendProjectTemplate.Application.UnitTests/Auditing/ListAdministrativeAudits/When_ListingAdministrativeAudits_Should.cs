using BackendProjectTemplate.Application.Auditing.Features.ListAdministrativeAudits;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Auditing.ListAdministrativeAudits;

public sealed class When_ListingAdministrativeAudits_Should
{
    [Fact]
    public async Task ReturnTheRepositoryPageForTheFilterAndPaging()
    {
        var repository = Substitute.For<IAdministrativeAuditReadModelRepository>();
        var filter = new AdministrativeAuditFilter("hopper", "users.locked", Guid.CreateVersion7(), "User", null, Guid.CreateVersion7(), null, null, null);
        var page = new AdministrativeAuditListPage([], 2, 10, 0);
        repository.ListAsync(new AdministrativeAuditListRequest(filter, 2, 10), Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListAdministrativeAuditsHandler(repository)
            .HandleAsync(new ListAdministrativeAuditsQuery(filter, 2, 10), CancellationToken.None);

        result.ShouldBeSameAs(page);
    }
}
