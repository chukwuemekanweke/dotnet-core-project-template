using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(
    TwoFactorVerificationMethod VerificationMethod,
    string Code,
    Guid CurrentSessionId,
    ActorContext ActorContext);
