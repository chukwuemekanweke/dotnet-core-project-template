namespace BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;

public sealed record RemovePasskeyCommand(Guid AppUserId, Guid StakeholderId, Guid TenantId, string CredentialId);
