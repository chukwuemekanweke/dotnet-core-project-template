using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Infrastructure.Authorization;

public sealed class IdentityUserPermissionService(UserManager<AppUser> userManager) : IUserPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken)
    {
        if (!ApplicationPermissions.All.Contains(permission))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return false;
        }

        var roles = await userManager.GetRolesAsync(user);
        cancellationToken.ThrowIfCancellationRequested();

        return RolePermissionMappings.ForRoles(roles).Contains(permission);
    }
}
