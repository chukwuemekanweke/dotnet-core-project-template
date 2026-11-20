using BackendProjectTemplate.Domain.Analytics.ReadModels;

namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed class ListActiveUsersHandler(IActiveUserAnalyticsReadModelRepository repository)
{
    public Task<ActiveUserListPage> HandleAsync(
        ActiveUserListQuery query,
        CancellationToken cancellationToken) =>
        repository.ListAsync(
            new ActiveUserReadModelListRequest(
                query.Date, query.TenantId, query.Page, query.PageSize),
            cancellationToken);
}
