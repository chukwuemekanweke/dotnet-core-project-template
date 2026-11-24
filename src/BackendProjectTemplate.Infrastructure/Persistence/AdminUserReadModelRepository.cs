using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AdminUserReadModelRepository(AppReadDbContext dbContext, TimeProvider timeProvider)
    : IAdminUserReadModelRepository
{
    public async Task<AdminUserListPage> ListAsync(
        AdminUserListRequest request,
        CancellationToken cancellationToken)
    {
        var users = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            var matchingStakeholderUserIds = dbContext.Stakeholders.AsNoTracking()
                .Where(stakeholder =>
                    EF.Functions.ILike(stakeholder.FirstName, pattern) ||
                    EF.Functions.ILike(stakeholder.LastName, pattern) ||
                    EF.Functions.ILike(stakeholder.FirstName + " " + stakeholder.LastName, pattern))
                .Select(stakeholder => stakeholder.AppUserId);
            users = users.Where(user =>
                (user.Email != null && EF.Functions.ILike(user.Email, pattern)) ||
                matchingStakeholderUserIds.Contains(user.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var roleName = request.Role;
            var matchingRoleUserIds =
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.Name == roleName
                select userRole.UserId;
            users = users.Where(user => matchingRoleUserIds.Contains(user.Id));
        }

        if (request.IsLocked.HasValue)
        {
            var now = timeProvider.GetUtcNow();
            users = request.IsLocked.Value
                ? users.Where(user => user.IsAdministrativelyLocked || user.LockoutEnd > now)
                : users.Where(user => !user.IsAdministrativelyLocked && (user.LockoutEnd == null || user.LockoutEnd <= now));
        }

        if (request.TenantId.HasValue)
        {
            var tenantUserIds = dbContext.Stakeholders.AsNoTracking()
                .Where(stakeholder => stakeholder.TenantId == request.TenantId.Value)
                .Select(stakeholder => stakeholder.AppUserId);
            users = users.Where(user => tenantUserIds.Contains(user.Id));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var rows = await (
                from user in users
                join stakeholder in dbContext.Stakeholders.AsNoTracking()
                    on user.Id equals stakeholder.AppUserId into stakeholders
                from stakeholder in stakeholders.DefaultIfEmpty()
                orderby user.CreatedAtUtc descending, user.Id descending
                select new
                {
                    User = new
                    {
                        user.Id,
                        Email = user.Email ?? string.Empty,
                        user.EmailConfirmed,
                        user.IsAdministrativelyLocked,
                        user.LockoutEnd,
                        user.CreatedAtUtc
                    },
                    Stakeholder = stakeholder == null
                        ? null
                        : new AdminStakeholderSummary(
                            stakeholder.Id,
                            stakeholder.TenantId,
                            stakeholder.FirstName,
                            stakeholder.LastName,
                            stakeholder.AvatarUrl,
                            stakeholder.IsVerified)
                })
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(row => row.User.Id).ToArray();
        var roles = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                orderby role.Name
                select new { userRole.UserId, RoleName = role.Name! })
            .ToListAsync(cancellationToken);
        var rolesByUser = roles.ToLookup(role => role.UserId, role => role.RoleName);

        var stakeholderIds = rows
            .Where(row => row.Stakeholder is not null)
            .Select(row => row.Stakeholder!.StakeholderId)
            .ToArray();
        var kycRows = await dbContext.KycVerifications.AsNoTracking()
            .Where(verification => stakeholderIds.Contains(verification.StakeholderId))
            .GroupBy(verification => verification.StakeholderId)
            .Select(group => group
                .OrderByDescending(verification => verification.StartedAtUtc)
                .ThenByDescending(verification => verification.Id)
                .Select(verification => new
                {
                    verification.StakeholderId,
                    Summary = new AdminKycSummary(
                        verification.Id,
                        verification.Status,
                        verification.StartedAtUtc,
                        verification.CompletedAtUtc)
                })
                .First())
            .ToListAsync(cancellationToken);
        var kycByStakeholder = kycRows.ToDictionary(row => row.StakeholderId, row => row.Summary);

        var items = rows.Select(row => new AdminUserListItem(
            row.User.Id,
            row.User.Email,
            row.User.EmailConfirmed,
            row.User.IsAdministrativelyLocked,
            row.User.IsAdministrativelyLocked ? null : row.User.LockoutEnd,
            row.User.CreatedAtUtc,
            row.Stakeholder,
            rolesByUser[row.User.Id].ToArray(),
            row.Stakeholder is not null && kycByStakeholder.TryGetValue(row.Stakeholder.StakeholderId, out var kyc)
                ? kyc
                : null)).ToArray();

        return new AdminUserListPage(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AdminUserDetail?> GetDetailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await (
                from user in dbContext.Users.AsNoTracking()
                where user.Id == userId
                join stakeholder in dbContext.Stakeholders.AsNoTracking()
                    on user.Id equals stakeholder.AppUserId into stakeholders
                from stakeholder in stakeholders.DefaultIfEmpty()
                select new
                {
                    User = new
                    {
                        user.Id,
                        Email = user.Email ?? string.Empty,
                        user.EmailConfirmed,
                        user.IsAdministrativelyLocked,
                        user.AdministrativelyLockedAtUtc,
                        user.LockoutEnd,
                        user.CreatedAtUtc,
                        user.UpdatedAtUtc
                    },
                    Stakeholder = stakeholder == null
                        ? null
                        : new AdminStakeholderSummary(
                            stakeholder.Id,
                            stakeholder.TenantId,
                            stakeholder.FirstName,
                            stakeholder.LastName,
                            stakeholder.AvatarUrl,
                            stakeholder.IsVerified)
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var roles = await GetRolesAsync(userId, cancellationToken) ?? [];
        if (row.Stakeholder is null)
        {
            return new AdminUserDetail(
                row.User.Id, row.User.Email, row.User.EmailConfirmed,
                row.User.IsAdministrativelyLocked, row.User.AdministrativelyLockedAtUtc,
                row.User.IsAdministrativelyLocked ? null : row.User.LockoutEnd,
                row.User.CreatedAtUtc, row.User.UpdatedAtUtc, roles, null, 0, null, null, null);
        }

        var stakeholderId = row.Stakeholder.StakeholderId;
        var now = timeProvider.GetUtcNow();
        var activeSessionCount = await dbContext.AuthenticationSessions.AsNoTracking()
            .CountAsync(session => session.StakeholderId == stakeholderId &&
                session.RevokedAtUtc == null && session.ExpiresAtUtc > now, cancellationToken);
        var latestLogin = await dbContext.SecurityActivities.AsNoTracking()
            .Where(activity => activity.StakeholderId == stakeholderId && activity.ActivityType == SecurityActivityType.InitialLogin)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenByDescending(activity => activity.Id)
            .Select(activity => new AdminLoginSummary(
                activity.OccurredAtUtc,
                activity.DeviceName,
                activity.DevicePlatform,
                activity.BrowserName))
            .FirstOrDefaultAsync(cancellationToken);
        var kyc = await dbContext.KycVerifications.AsNoTracking()
            .Where(verification => verification.StakeholderId == stakeholderId)
            .OrderByDescending(verification => verification.StartedAtUtc)
            .ThenByDescending(verification => verification.Id)
            .Select(verification => new AdminKycSummary(
                verification.Id,
                verification.Status,
                verification.StartedAtUtc,
                verification.CompletedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
        var subscription = await (
                from billingAccount in dbContext.BillingAccounts.AsNoTracking()
                where billingAccount.OwnerType == BillingAccountOwnerType.Stakeholder &&
                    billingAccount.StakeholderId == stakeholderId
                join item in dbContext.Subscriptions.AsNoTracking()
                    on billingAccount.Id equals item.BillingAccountId
                join plan in dbContext.SubscriptionPlans.AsNoTracking()
                    on item.PlanId equals plan.Id
                orderby item.CreatedAtUtc descending, item.Id descending
                select new AdminSubscriptionSummary(item.Id, plan.Code, item.Status, item.EntitlementEndUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return new AdminUserDetail(
            row.User.Id, row.User.Email, row.User.EmailConfirmed,
            row.User.IsAdministrativelyLocked, row.User.AdministrativelyLockedAtUtc,
            row.User.IsAdministrativelyLocked ? null : row.User.LockoutEnd,
            row.User.CreatedAtUtc, row.User.UpdatedAtUtc, roles, row.Stakeholder,
            activeSessionCount, latestLogin, kyc, subscription);
    }

    public async Task<IReadOnlyList<string>?> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return null;
        }

        return await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId
                orderby role.Name
                select role.Name!)
            .ToListAsync(cancellationToken);
    }
}
