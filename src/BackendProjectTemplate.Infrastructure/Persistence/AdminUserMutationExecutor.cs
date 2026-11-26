using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Authentication;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

internal sealed record AdminSecurityMutationContext(
    Guid ActorUserId,
    Guid TargetUserId,
    string Reason,
    ActorContext ActorContext,
    Guid? SessionId = null);

internal sealed class AdminUserMutationExecutor(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    IAdministrativeAuditRecorder auditRecorder,
    TimeProvider timeProvider)
{
    private const long AdministratorMutationLockId = 1_014_036_517;

    public async Task<AdminUserMutationResult> ExecuteAsync(
        AdminSecurityMutationContext context,
        AdministrativeSecurityAction action,
        Func<AppUser, IReadOnlyList<string>, DateTimeOffset, Task<AdminUserMutationResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({AdministratorMutationLockId});",
            cancellationToken);

        var user = await userManager.FindByIdAsync(context.TargetUserId.ToString());
        if (user is null || user.IsDeleted)
        {
            return Failure(AdminUserMutationStatus.UserNotFound, "User was not found.");
        }

        var previousRoles = (await userManager.GetRolesAsync(user))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var now = timeProvider.GetUtcNow();
        var result = await operation(user, previousRoles, now);
        if (result.Status is not (AdminUserMutationStatus.Succeeded or AdminUserMutationStatus.Unchanged))
        {
            return result;
        }

        if (result.Status == AdminUserMutationStatus.Succeeded)
        {
            var resultingRoles = result.Roles ?? previousRoles;
            dbContext.AdministrativeSecurityAudits.Add(AdministrativeSecurityAudit.CreateSucceeded(
                context.ActorUserId,
                context.TargetUserId,
                action,
                context.Reason,
                now,
                context.ActorContext.TenantId,
                context.ActorContext.CorrelationId,
                context.ActorContext.FlowId,
                previousRoles,
                resultingRoles));
            await auditRecorder.RecordAsync(
                await CreateAdministrativeAuditEntryAsync(context, action, previousRoles, resultingRoles, cancellationToken),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Maps a successful user-security mutation to the unified administrative audit journal. Identity users and
    /// roles are platform-wide, so user targets are global; a single revoked session belongs to the tenant of
    /// the stakeholder that owns it. Revoking all sessions is recorded once against the user.
    /// </summary>
    private async Task<AdministrativeAuditEntry> CreateAdministrativeAuditEntryAsync(
        AdminSecurityMutationContext context,
        AdministrativeSecurityAction action,
        IReadOnlyList<string> previousRoles,
        IReadOnlyList<string> resultingRoles,
        CancellationToken cancellationToken)
    {
        var targetDisplayName = await dbContext.Stakeholders.AsNoTracking()
            .Where(stakeholder => stakeholder.AppUserId == context.TargetUserId)
            .OrderBy(stakeholder => stakeholder.CreatedAtUtc)
            .ThenBy(stakeholder => stakeholder.Id)
            .Select(stakeholder => stakeholder.FirstName + " " + stakeholder.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        AdministrativeAuditEntry UserEntry(string auditAction, AdministrativeAuditChanges? changes) =>
            new(
                context.ActorContext,
                auditAction,
                AdministrativeAuditTargetTypes.User,
                context.TargetUserId,
                targetDisplayName,
                null,
                context.Reason,
                changes,
                context.ActorUserId);

        AdministrativeAuditChanges RoleChanges() =>
            new AdministrativeAuditChanges().Field("roles", previousRoles, resultingRoles);

        switch (action)
        {
            case AdministrativeSecurityAction.LockUser:
                return UserEntry(
                    AdministrativeAuditActions.Users.Locked,
                    new AdministrativeAuditChanges().Field("isAdministrativelyLocked", false, true));
            case AdministrativeSecurityAction.UnlockUser:
                return UserEntry(
                    AdministrativeAuditActions.Users.Unlocked,
                    new AdministrativeAuditChanges().Field("isAdministrativelyLocked", true, false));
            case AdministrativeSecurityAction.AssignRole:
                return UserEntry(AdministrativeAuditActions.Users.RoleAssigned, RoleChanges());
            case AdministrativeSecurityAction.RemoveRole:
                return UserEntry(AdministrativeAuditActions.Users.RoleRemoved, RoleChanges());
            case AdministrativeSecurityAction.RevokeAllSessions:
                return UserEntry(AdministrativeAuditActions.Users.AllSessionsRevoked, null);
            case AdministrativeSecurityAction.RevokeSession:
                var sessionId = context.SessionId
                    ?? throw new InvalidOperationException("A session identifier is required to audit a session revocation.");
                var sessionTenantId = await (
                        from session in dbContext.AuthenticationSessions.AsNoTracking()
                        join stakeholder in dbContext.Stakeholders.AsNoTracking()
                            on session.StakeholderId equals stakeholder.Id
                        where session.Id == sessionId
                        select stakeholder.TenantId)
                    .SingleAsync(cancellationToken);
                return new AdministrativeAuditEntry(
                    context.ActorContext,
                    AdministrativeAuditActions.Users.SessionRevoked,
                    AdministrativeAuditTargetTypes.AuthenticationSession,
                    sessionId,
                    targetDisplayName,
                    sessionTenantId,
                    context.Reason,
                    new AdministrativeAuditChanges().Field("isRevoked", false, true),
                    context.ActorUserId);
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported administrative security action.");
        }
    }

    public static AdminUserMutationResult Failure(AdminUserMutationStatus status, string error) =>
        new(status, Error: error);

    public static AdminUserMutationResult IdentityFailure(IdentityResult result) =>
        Failure(
            AdminUserMutationStatus.IdentityFailure,
            string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
}
