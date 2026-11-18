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
    ActorContext ActorContext);

internal sealed class AdminUserMutationExecutor(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
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
                result.Roles ?? previousRoles));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public static AdminUserMutationResult Failure(AdminUserMutationStatus status, string error) =>
        new(status, Error: error);

    public static AdminUserMutationResult IdentityFailure(IdentityResult result) =>
        Failure(
            AdminUserMutationStatus.IdentityFailure,
            string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
}
