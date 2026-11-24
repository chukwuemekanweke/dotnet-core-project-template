using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class ActiveUserAnalyticsReadModelRepository(AppReadDbContext dbContext)
    : IActiveUserAnalyticsReadModelRepository
{
    public async Task<ActiveUserAnalyticsSummary> GetSummaryAsync(
        DateOnly date,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var activities = ApplyTenant(dbContext.UserDailyActivities.AsNoTracking(), tenantId);
        var monthlyFrom = date.AddDays(-29);
        var rows = await activities
            .Where(activity => activity.ActivityDate >= monthlyFrom && activity.ActivityDate <= date)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Daily = group.Where(activity => activity.ActivityDate == date)
                    .Select(activity => activity.StakeholderId).Distinct().LongCount(),
                Weekly = group.Where(activity => activity.ActivityDate >= date.AddDays(-6))
                    .Select(activity => activity.StakeholderId).Distinct().LongCount(),
                Monthly = group.Select(activity => activity.StakeholderId).Distinct().LongCount()
            })
            .SingleOrDefaultAsync(cancellationToken);
        return new ActiveUserAnalyticsSummary(
            rows?.Daily ?? 0,
            rows?.Weekly ?? 0,
            rows?.Monthly ?? 0);
    }

    public async Task<IReadOnlyList<ActiveUserAnalyticsDailyRow>> GetTimelineAsync(
        DateOnly from,
        DateOnly to,
        Guid? tenantId,
        CancellationToken cancellationToken) =>
        await ApplyTenant(dbContext.UserDailyActivities.AsNoTracking(), tenantId)
            .Where(activity => activity.ActivityDate >= from && activity.ActivityDate <= to)
            .GroupBy(activity => activity.ActivityDate)
            .OrderBy(group => group.Key)
            .Select(group => new ActiveUserAnalyticsDailyRow(
                group.Key,
                group.Select(activity => activity.StakeholderId).Distinct().LongCount()))
            .ToListAsync(cancellationToken);

    public async Task<ActiveUserListPage> ListAsync(
        ActiveUserReadModelListRequest request,
        CancellationToken cancellationToken)
    {
        var activities = ApplyTenant(dbContext.UserDailyActivities.AsNoTracking(), request.TenantId)
            .Where(activity => activity.ActivityDate == request.Date);
        var visible =
            from activity in activities
            join stakeholder in dbContext.Stakeholders.AsNoTracking() on activity.StakeholderId equals stakeholder.Id
            join user in dbContext.Users.AsNoTracking() on stakeholder.AppUserId equals user.Id
            select new { activity, stakeholder, user };
        var totalCount = await visible.CountAsync(cancellationToken);
        var items = await visible
            .OrderByDescending(row => row.activity.LastActivityAtUtc)
            .ThenByDescending(row => row.stakeholder.Id)
            .Select(row => new ActiveUserListItem(
                row.stakeholder.Id,
                row.user.Id,
                row.stakeholder.AvatarUrl,
                row.stakeholder.FirstName + " " + row.stakeholder.LastName,
                row.user.Email ?? string.Empty,
                row.activity.FirstActivityAtUtc,
                row.activity.LastActivityAtUtc,
                row.activity.ActivityCount,
                dbContext.SecurityActivities.AsNoTracking()
                    .Where(login => login.TenantId == row.activity.TenantId &&
                        login.StakeholderId == row.stakeholder.Id &&
                        login.ActivityType == SecurityActivityType.InitialLogin)
                    .Select(login => (DateTimeOffset?)login.OccurredAtUtc)
                    .Max()))
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new ActiveUserListPage(
            items, request.Page, request.PageSize, totalCount);
    }

    private static IQueryable<UserDailyActivity> ApplyTenant(
        IQueryable<UserDailyActivity> query,
        Guid? tenantId) =>
        tenantId.HasValue ? query.Where(activity => activity.TenantId == tenantId.Value) : query;
}
