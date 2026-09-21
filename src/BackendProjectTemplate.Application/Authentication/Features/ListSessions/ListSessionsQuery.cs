namespace BackendProjectTemplate.Application.Authentication.Features.ListSessions;

public sealed record ListSessionsQuery(Guid AppUserId, Guid StakeholderId, Guid TenantId, Guid CurrentSessionId);
