using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;

public sealed record VerifyStepUpMfaCommand(
    TwoFactorVerificationMethod VerificationMethod,
    string Code,
    string Operation,
    string RequestFingerprint,
    Guid SessionId,
    ActorContext ActorContext);
