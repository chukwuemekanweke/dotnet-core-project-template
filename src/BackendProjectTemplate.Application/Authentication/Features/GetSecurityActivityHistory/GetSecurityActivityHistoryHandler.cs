using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Authentication.ReadModels;

namespace BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;

public sealed class GetSecurityActivityHistoryHandler(ISecurityActivityReadModelRepository securityActivityReadModelRepository)
{
    public async Task<GetSecurityActivityHistoryResult> HandleAsync(
        GetSecurityActivityHistoryCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        var tenantId = command.ActorContext.TenantId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty ||
            !tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return new GetSecurityActivityHistoryResult(
                GetSecurityActivityHistoryStatus.NotAuthenticated,
                [],
                null);
        }

        var (cursorOccurredAtUtc, cursorSecurityActivityId) = CursorPagination.Decode(command.Cursor);
        var page = await securityActivityReadModelRepository.GetByStakeholderAsync(
            new SecurityActivityHistoryCursorRequest(
                tenantId.Value,
                stakeholderId.Value,
                cursorOccurredAtUtc,
                cursorSecurityActivityId,
                command.Limit),
            cancellationToken);

        var activities = page.Activities
            .Select(activity => new GetSecurityActivityHistoryResponse(
                activity.SecurityActivityId,
                activity.ActivityType.ToString(),
                activity.OccurredAtUtc,
                activity.IpAddress,
                activity.DeviceName,
                activity.DevicePlatform,
                activity.BrowserName,
                activity.City,
                activity.State,
                activity.Country,
                activity.FailureReason))
            .ToArray();

        var nextCursor = page.HasMore && page.Activities.Count > 0
            ? CursorPagination.Encode(
                page.Activities[^1].OccurredAtUtc,
                page.Activities[^1].SecurityActivityId)
            : null;

        return new GetSecurityActivityHistoryResult(
            GetSecurityActivityHistoryStatus.Success,
            activities,
            nextCursor);
    }
}
