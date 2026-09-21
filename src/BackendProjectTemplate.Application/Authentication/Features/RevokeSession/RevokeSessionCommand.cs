namespace BackendProjectTemplate.Application.Authentication.Features.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId, Guid AppUserId, Guid StakeholderId, Guid TenantId);
