using BackendProjectTemplate.Domain.Authentication.Administration;

namespace BackendProjectTemplate.Application.Authentication.Features.GetAdminUserRoles;

public sealed record GetAdminUserRolesQuery(Guid UserId);
public sealed record GetAdminUserRolesResponse(Guid UserId, IReadOnlyList<string> Roles);

public sealed class GetAdminUserRolesHandler(IAdminUserReadModelRepository repository)
{
    public async Task<GetAdminUserRolesResponse?> HandleAsync(
        GetAdminUserRolesQuery query,
        CancellationToken cancellationToken)
    {
        var roles = await repository.GetRolesAsync(query.UserId, cancellationToken);
        return roles is null ? null : new GetAdminUserRolesResponse(query.UserId, roles);
    }
}
