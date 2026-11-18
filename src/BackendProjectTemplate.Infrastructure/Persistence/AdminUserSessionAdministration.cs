using BackendProjectTemplate.Domain.Authentication;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Services;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed class AdminUserSessionAdministration(
    AdminUserMutationExecutor executor,
    AppDbContext dbContext,
    IAuthenticationSessionService sessionService,
    TimeProvider timeProvider) : IAdminUserSessionAdministration
{
    public Task<AdminUserMutationResult> RevokeAsync(
        AdminUserSessionRequest request,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(ToContext(request), AdministrativeSecurityAction.RevokeSession,
            async (user, roles, _) =>
            {
                var stakeholderIds = await GetStakeholderIdsAsync(user.Id, cancellationToken);
                var session = await dbContext.AuthenticationSessions.AsNoTracking()
                    .SingleOrDefaultAsync(item =>
                        item.Id == request.SessionId && stakeholderIds.Contains(item.StakeholderId),
                        cancellationToken);
                if (session is null)
                {
                    return AdminUserMutationExecutor.Failure(
                        AdminUserMutationStatus.SessionNotFound,
                        "Session was not found for the target user.");
                }

                if (session.RevokedAtUtc.HasValue)
                {
                    return new AdminUserMutationResult(AdminUserMutationStatus.Unchanged, roles);
                }

                await sessionService.RevokeAsync(request.SessionId, session.StakeholderId, cancellationToken);
                return new AdminUserMutationResult(AdminUserMutationStatus.Succeeded, roles);
            }, cancellationToken);

    public Task<AdminUserMutationResult> RevokeAllAsync(
        AdminUserAccountRequest request,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(ToContext(request), AdministrativeSecurityAction.RevokeAllSessions,
            async (user, roles, _) =>
            {
                var stakeholderIds = await GetStakeholderIdsAsync(user.Id, cancellationToken);
                var now = timeProvider.GetUtcNow();
                var hasActiveSessions = await dbContext.AuthenticationSessions.AsNoTracking()
                    .AnyAsync(session => stakeholderIds.Contains(session.StakeholderId) &&
                        session.RevokedAtUtc == null && session.ExpiresAtUtc > now, cancellationToken);
                if (!hasActiveSessions)
                {
                    return new AdminUserMutationResult(AdminUserMutationStatus.Unchanged, roles);
                }

                foreach (var stakeholderId in stakeholderIds)
                {
                    await sessionService.RevokeAllAsync(stakeholderId, cancellationToken);
                }

                return new AdminUserMutationResult(AdminUserMutationStatus.Succeeded, roles);
            }, cancellationToken);

    private Task<Guid[]> GetStakeholderIdsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Stakeholders
            .Where(stakeholder => stakeholder.AppUserId == userId)
            .Select(stakeholder => stakeholder.Id)
            .ToArrayAsync(cancellationToken);

    private static AdminSecurityMutationContext ToContext(AdminUserSessionRequest request) =>
        new(request.ActorUserId, request.TargetUserId, request.Reason, request.ActorContext);

    private static AdminSecurityMutationContext ToContext(AdminUserAccountRequest request) =>
        new(request.ActorUserId, request.TargetUserId, request.Reason, request.ActorContext);
}
