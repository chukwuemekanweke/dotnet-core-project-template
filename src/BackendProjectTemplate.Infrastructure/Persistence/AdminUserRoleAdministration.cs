using BackendProjectTemplate.Domain.Authentication;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed class AdminUserRoleAdministration(
    AdminUserMutationExecutor executor,
    AdministratorSafetyGuard safetyGuard,
    UserManager<AppUser> userManager) : IAdminUserRoleAdministration
{
    public Task<AdminUserMutationResult> AssignAsync(
        AdminUserRoleRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(request, assign: true, cancellationToken);

    public Task<AdminUserMutationResult> RemoveAsync(
        AdminUserRoleRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(request, assign: false, cancellationToken);

    private Task<AdminUserMutationResult> ChangeAsync(
        AdminUserRoleRequest request,
        bool assign,
        CancellationToken cancellationToken)
    {
        if (!ApplicationRoles.All.Contains(request.RoleName, StringComparer.Ordinal))
        {
            return Task.FromResult(AdminUserMutationExecutor.Failure(
                AdminUserMutationStatus.UnknownRole,
                "Role is not in the application role catalog."));
        }

        var action = assign ? AdministrativeSecurityAction.AssignRole : AdministrativeSecurityAction.RemoveRole;
        var context = new AdminSecurityMutationContext(
            request.ActorUserId, request.TargetUserId, request.Reason, request.ActorContext);
        return executor.ExecuteAsync(context, action, async (user, previousRoles, _) =>
        {
            var isAssigned = previousRoles.Contains(request.RoleName, StringComparer.Ordinal);
            if (assign == isAssigned)
            {
                return new AdminUserMutationResult(AdminUserMutationStatus.Unchanged, previousRoles);
            }

            if (!assign && request.RoleName == ApplicationRoles.Administrator)
            {
                if (request.ActorUserId == request.TargetUserId)
                {
                    return AdminUserMutationExecutor.Failure(
                        AdminUserMutationStatus.SelfProtection,
                        "Administrators cannot remove their own Administrator role.");
                }

                if (safetyGuard.IsUsable(user) &&
                    await safetyGuard.CountUsableAsync(cancellationToken) <= 1)
                {
                    return AdminUserMutationExecutor.Failure(
                        AdminUserMutationStatus.LastAdministrator,
                        "The last usable Administrator role cannot be removed.");
                }
            }

            var identityResult = assign
                ? await userManager.AddToRoleAsync(user, request.RoleName)
                : await userManager.RemoveFromRoleAsync(user, request.RoleName);
            if (!identityResult.Succeeded)
            {
                return AdminUserMutationExecutor.IdentityFailure(identityResult);
            }

            var resultingRoles = assign
                ? previousRoles.Append(request.RoleName).Order(StringComparer.Ordinal).ToArray()
                : previousRoles.Where(role => role != request.RoleName).ToArray();
            return new AdminUserMutationResult(AdminUserMutationStatus.Succeeded, resultingRoles);
        }, cancellationToken);
    }
}
