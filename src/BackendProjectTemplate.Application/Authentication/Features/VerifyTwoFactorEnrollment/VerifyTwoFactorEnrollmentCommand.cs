using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed record VerifyTwoFactorEnrollmentCommand(
    string Code,
    Guid CurrentSessionId,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
