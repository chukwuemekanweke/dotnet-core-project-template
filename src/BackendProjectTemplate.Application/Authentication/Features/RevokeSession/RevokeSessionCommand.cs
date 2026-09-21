namespace BackendProjectTemplate.Application.Authentication.Features.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId, Guid StakeholderId);
