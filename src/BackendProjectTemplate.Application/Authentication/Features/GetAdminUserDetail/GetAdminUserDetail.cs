using BackendProjectTemplate.Domain.Authentication.Administration;

namespace BackendProjectTemplate.Application.Authentication.Features.GetAdminUserDetail;

public sealed record GetAdminUserDetailQuery(Guid UserId);

public sealed class GetAdminUserDetailHandler(IAdminUserReadModelRepository repository)
{
    public Task<AdminUserDetail?> HandleAsync(GetAdminUserDetailQuery query, CancellationToken cancellationToken) =>
        repository.GetDetailAsync(query.UserId, cancellationToken);
}
