using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed record RegenerateTwoFactorRecoveryCodesCommand(
    TwoFactorVerificationMethod VerificationMethod,
    string Code,
    Guid CurrentSessionId,
    ActorContext ActorContext);
