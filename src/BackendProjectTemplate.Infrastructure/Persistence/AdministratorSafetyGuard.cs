using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed class AdministratorSafetyGuard(AppDbContext dbContext, TimeProvider timeProvider)
{
    public Task<int> CountUsableAsync(CancellationToken cancellationToken)
    {
        var administratorRoleId = ApplicationRoles.GetId(ApplicationRoles.Administrator);
        var now = timeProvider.GetUtcNow();
        return (
                from userRole in dbContext.UserRoles
                join user in dbContext.Users on userRole.UserId equals user.Id
                where userRole.RoleId == administratorRoleId &&
                    user.EmailConfirmed &&
                    !user.IsAdministrativelyLocked &&
                    (user.LockoutEnd == null || user.LockoutEnd <= now)
                select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public bool IsUsable(AppUser user)
    {
        var now = timeProvider.GetUtcNow();
        return user.EmailConfirmed &&
            !user.IsDeleted &&
            !user.IsAdministrativelyLocked &&
            (!user.LockoutEnd.HasValue || user.LockoutEnd <= now);
    }
}
