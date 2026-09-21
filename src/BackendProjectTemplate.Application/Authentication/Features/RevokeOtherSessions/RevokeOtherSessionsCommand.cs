namespace BackendProjectTemplate.Application.Authentication.Features.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(Guid CurrentSessionId, Guid AppUserId,
    Guid StakeholderId, Guid TenantId);
