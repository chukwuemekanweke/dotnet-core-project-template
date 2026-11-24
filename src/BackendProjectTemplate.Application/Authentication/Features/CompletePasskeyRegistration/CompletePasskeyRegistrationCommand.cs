namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeyRegistration;

public sealed record CompletePasskeyRegistrationCommand(
    Guid AppUserId,
    Guid StakeholderId,
    Guid TenantId,
    string FlowToken,
    string CredentialJson,
    string IpAddress,
    string UserAgent);
