namespace BackendProjectTemplate.Application.Authentication.Features.RenamePasskey;

public sealed record RenamePasskeyCommand(
    Guid AppUserId,
    Guid StakeholderId,
    Guid TenantId,
    string CredentialId,
    string Name);
