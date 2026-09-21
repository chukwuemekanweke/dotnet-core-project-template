using BackendProjectTemplate.Domain.Authentication.Services;

namespace BackendProjectTemplate.Application.Authentication.Features.ListSessions;

public sealed class ListSessionsHandler(IAuthenticationSessionService sessionService)
{
    public async Task<IReadOnlyList<ActiveSessionResult>> HandleAsync(
        ListSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await sessionService.ListActiveAsync(query.AppUserId, cancellationToken);
        return sessions.Where(session => session.StakeholderId == query.StakeholderId &&
                session.TenantId == query.TenantId)
            .OrderByDescending(session => session.Id == query.CurrentSessionId)
            .ThenByDescending(session => session.LastActiveAtUtc)
            .Select(session => new ActiveSessionResult(session.Id, session.DeviceName,
                session.DevicePlatform, session.BrowserName, session.UserAgent,
                session.FirstIpAddress.Value, session.LastIpAddress.Value,
                session.CreatedAtUtc, session.LastActiveAtUtc, session.ExpiresAtUtc,
                session.Id == query.CurrentSessionId)).ToArray();
    }
}
