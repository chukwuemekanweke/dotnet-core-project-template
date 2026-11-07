namespace BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyRegistrationOptions;

public sealed record CreatePasskeyRegistrationOptionsCommand(
    Guid AppUserId,
    Guid StakeholderId,
    Guid TenantId,
    string PasskeyName);
