using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class SecurityActivityReadModelRepository(AppReadDbContext dbContext) : ISecurityActivityReadModelRepository
{
    public async Task<SecurityActivityHistoryCursorPage> GetByStakeholderAsync(
        SecurityActivityHistoryCursorRequest request,
        CancellationToken cancellationToken)
    {
        var query =
            from securityActivity in dbContext.SecurityActivities.AsNoTracking()
            join ipAddress in dbContext.IpAddresses.AsNoTracking()
                on securityActivity.IpAddressId equals ipAddress.Id
            join ipAddressLocation in dbContext.IpAddressLocations.AsNoTracking()
                on securityActivity.IpAddressLocationId equals ipAddressLocation.Id into locations
            from ipAddressLocation in locations.DefaultIfEmpty()
            where securityActivity.TenantId == request.TenantId &&
                securityActivity.StakeholderId == request.StakeholderId &&
                securityActivity.ActivityType != SecurityActivityType.TokenRefresh
            select new
            {
                securityActivity.Id,
                securityActivity.ActivityType,
                securityActivity.OccurredAtUtc,
                IpAddress = ipAddress.Value,
                securityActivity.DeviceName,
                securityActivity.DevicePlatform,
                securityActivity.BrowserName,
                City = ipAddressLocation == null ? null : ipAddressLocation.City,
                State = ipAddressLocation == null ? null : ipAddressLocation.State,
                Country = ipAddressLocation == null ? null : ipAddressLocation.Country,
                securityActivity.FailureReason
            };

        if (request.CursorOccurredAtUtc.HasValue && request.CursorSecurityActivityId.HasValue)
        {
            var cursorOccurredAtUtc = request.CursorOccurredAtUtc.Value;
            var cursorSecurityActivityId = request.CursorSecurityActivityId.Value;
            query = query.Where(activity =>
                activity.OccurredAtUtc < cursorOccurredAtUtc ||
                (activity.OccurredAtUtc == cursorOccurredAtUtc &&
                    activity.Id.CompareTo(cursorSecurityActivityId) < 0));
        }

        var activities = await query
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenByDescending(activity => activity.Id)
            .Take(request.Limit + 1)
            .Select(activity => new SecurityActivityHistoryReadModel(
                activity.Id,
                activity.ActivityType,
                activity.OccurredAtUtc,
                activity.IpAddress,
                activity.DeviceName,
                activity.DevicePlatform,
                activity.BrowserName,
                activity.City,
                activity.State,
                activity.Country,
                activity.FailureReason))
            .ToListAsync(cancellationToken);

        var hasMore = activities.Count > request.Limit;
        if (hasMore)
        {
            activities.RemoveAt(activities.Count - 1);
        }

        return new SecurityActivityHistoryCursorPage(activities, hasMore);
    }
}
