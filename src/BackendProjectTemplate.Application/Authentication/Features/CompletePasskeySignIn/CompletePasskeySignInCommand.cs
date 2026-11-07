using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;

public sealed record CompletePasskeySignInCommand(
    Guid TenantId,
    string FlowToken,
    string CredentialJson,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
