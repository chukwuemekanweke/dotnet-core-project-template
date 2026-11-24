using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class RegistrationAnalyticsReadModelRepository(AppReadDbContext dbContext)
    : IRegistrationAnalyticsReadModelRepository
{
    public async Task<RegistrationAnalyticsCounts> GetCountsAsync(
        RegistrationAnalyticsFilter filter,
        DateTimeOffset previousFromUtc,
        CancellationToken cancellationToken)
    {
        var stakeholders = ApplyTenant(
            dbContext.Stakeholders.IgnoreQueryFilters().AsNoTracking(), filter.TenantId);
        var counts = await stakeholders
            .Where(stakeholder => stakeholder.CreatedAtUtc < filter.ToExclusiveUtc)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                AllTime = group.LongCount(),
                Current = group.LongCount(stakeholder => stakeholder.CreatedAtUtc >= filter.FromUtc),
                Previous = group.LongCount(stakeholder =>
                    stakeholder.CreatedAtUtc >= previousFromUtc &&
                    stakeholder.CreatedAtUtc < filter.FromUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);
        return new RegistrationAnalyticsCounts(
            counts?.AllTime ?? 0,
            counts?.Current ?? 0,
            counts?.Previous ?? 0);
    }

    public Task<IReadOnlyList<RegistrationAnalyticsTimelineRow>> GetTimelineAsync(
        RegistrationAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var stakeholders = ApplyTenant(
                dbContext.Stakeholders.IgnoreQueryFilters().AsNoTracking(), filter.TenantId)
            .Where(stakeholder => stakeholder.CreatedAtUtc >= filter.FromUtc &&
                stakeholder.CreatedAtUtc < filter.ToExclusiveUtc);
        return interval switch
        {
            Interval.Day => GroupAsync(stakeholders.Select(stakeholder => new TimelineSource
            {
                PeriodStart = stakeholder.CreatedAtUtc.Date
            }), cancellationToken),
            Interval.Week => GroupAsync(stakeholders.Select(stakeholder => new TimelineSource
            {
                PeriodStart = stakeholder.CreatedAtUtc.Date.AddDays(-(((int)stakeholder.CreatedAtUtc.DayOfWeek + 6) % 7))
            }), cancellationToken),
            Interval.Month => GroupAsync(stakeholders.Select(stakeholder => new TimelineSource
            {
                PeriodStart = new DateTime(stakeholder.CreatedAtUtc.Year, stakeholder.CreatedAtUtc.Month, 1)
            }), cancellationToken),
            Interval.Year => GroupAsync(stakeholders.Select(stakeholder => new TimelineSource
            {
                PeriodStart = new DateTime(stakeholder.CreatedAtUtc.Year, 1, 1)
            }), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }

    public async Task<RegisteredUserListPage> ListAsync(
        RegisteredUserListRequest request,
        CancellationToken cancellationToken)
    {
        var stakeholders = ApplyTenant(dbContext.Stakeholders.AsNoTracking(), request.TenantId)
            .Where(stakeholder => stakeholder.CreatedAtUtc >= request.FromUtc &&
                stakeholder.CreatedAtUtc < request.ToExclusiveUtc);
        var totalCount = await stakeholders.CountAsync(cancellationToken);
        var items = await (
                from stakeholder in stakeholders
                join user in dbContext.Users.AsNoTracking() on stakeholder.AppUserId equals user.Id
                orderby stakeholder.CreatedAtUtc descending, stakeholder.Id descending
                select new RegisteredUserListItem(
                    stakeholder.Id,
                    user.Id,
                    stakeholder.AvatarUrl,
                    stakeholder.FirstName + " " + stakeholder.LastName,
                    user.Email ?? string.Empty,
                    stakeholder.CreatedAtUtc,
                    dbContext.SecurityActivities.AsNoTracking()
                        .Where(login => login.TenantId == stakeholder.TenantId &&
                            login.StakeholderId == stakeholder.Id &&
                            login.ActivityType == SecurityActivityType.InitialLogin)
                        .Select(login => (DateTimeOffset?)login.OccurredAtUtc)
                        .Max()))
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new RegisteredUserListPage(
            items, request.Page, request.PageSize, totalCount);
    }

    private static IQueryable<Stakeholder> ApplyTenant(IQueryable<Stakeholder> query, Guid? tenantId) =>
        tenantId.HasValue ? query.Where(stakeholder => stakeholder.TenantId == tenantId.Value) : query;

    private static async Task<IReadOnlyList<RegistrationAnalyticsTimelineRow>> GroupAsync(
        IQueryable<TimelineSource> source,
        CancellationToken cancellationToken)
    {
        var rows = await source
            .GroupBy(item => item.PeriodStart)
            .Select(group => new { PeriodStart = group.Key, Count = group.LongCount() })
            .OrderBy(row => row.PeriodStart)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new RegistrationAnalyticsTimelineRow(
            DateOnly.FromDateTime(row.PeriodStart), row.Count)).ToArray();
    }

    private sealed class TimelineSource
    {
        public DateTime PeriodStart { get; init; }
    }
}
