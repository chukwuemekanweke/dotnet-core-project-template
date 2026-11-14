namespace BackendProjectTemplate.Domain.Common.Authorization;

public interface IUserPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
