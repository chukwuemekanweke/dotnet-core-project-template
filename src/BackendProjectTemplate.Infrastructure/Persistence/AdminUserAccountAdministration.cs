using BackendProjectTemplate.Domain.Authentication;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed class AdminUserAccountAdministration(
    AdminUserMutationExecutor executor,
    AdministratorSafetyGuard safetyGuard,
    AppDbContext dbContext,
    UserManager<Domain.Authentication.Entities.AppUser> userManager,
    IAuthenticationSessionService sessionService) : IAdminUserAccountAdministration
{
    public Task<AdminUserMutationResult> LockAsync(
        AdminUserAccountRequest request,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(ToContext(request), AdministrativeSecurityAction.LockUser,
            async (user, roles, now) =>
            {
                if (request.ActorUserId == request.TargetUserId)
                {
                    return AdminUserMutationExecutor.Failure(
                        AdminUserMutationStatus.SelfProtection,
                        "Administrators cannot lock their own account.");
                }

                if (user.IsAdministrativelyLocked)
                {
                    return new AdminUserMutationResult(AdminUserMutationStatus.Unchanged, roles);
                }

                if (roles.Contains(ApplicationRoles.Administrator, StringComparer.Ordinal) &&
                    safetyGuard.IsUsable(user) &&
                    await safetyGuard.CountUsableAsync(cancellationToken) <= 1)
                {
                    return AdminUserMutationExecutor.Failure(
                        AdminUserMutationStatus.LastAdministrator,
                        "The last usable Administrator cannot be locked.");
                }

                user.LockAdministratively(request.ActorUserId, now);
                var identityResult = await userManager.UpdateAsync(user);
                if (!identityResult.Succeeded)
                {
                    return AdminUserMutationExecutor.IdentityFailure(identityResult);
                }

                identityResult = await userManager.UpdateSecurityStampAsync(user);
                if (!identityResult.Succeeded)
                {
                    return AdminUserMutationExecutor.IdentityFailure(identityResult);
                }

                await RevokeAllSessionsAsync(user.Id, cancellationToken);
                return new AdminUserMutationResult(AdminUserMutationStatus.Succeeded, roles);
            }, cancellationToken);

    public Task<AdminUserMutationResult> UnlockAsync(
        AdminUserAccountRequest request,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(ToContext(request), AdministrativeSecurityAction.UnlockUser,
            async (user, roles, _) =>
            {
                if (!user.UnlockAdministratively())
                {
                    return new AdminUserMutationResult(AdminUserMutationStatus.Unchanged, roles);
                }

                var identityResult = await userManager.UpdateAsync(user);
                return identityResult.Succeeded
                    ? new AdminUserMutationResult(AdminUserMutationStatus.Succeeded, roles)
                    : AdminUserMutationExecutor.IdentityFailure(identityResult);
            }, cancellationToken);

    private async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var stakeholderIds = await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.AppUserId == userId)
            .Select(stakeholder => stakeholder.Id)
            .ToArrayAsync(cancellationToken);
        foreach (var stakeholderId in stakeholderIds)
        {
            await sessionService.RevokeAllAsync(stakeholderId, cancellationToken);
        }
    }

    private static AdminSecurityMutationContext ToContext(AdminUserAccountRequest request) =>
        new(request.ActorUserId, request.TargetUserId, request.Reason, request.ActorContext);
}
