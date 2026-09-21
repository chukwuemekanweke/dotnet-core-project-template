namespace BackendProjectTemplate.Application.Authentication.Features.ListSessions;

public sealed record ListSessionsQuery(Guid StakeholderId, Guid CurrentSessionId);
