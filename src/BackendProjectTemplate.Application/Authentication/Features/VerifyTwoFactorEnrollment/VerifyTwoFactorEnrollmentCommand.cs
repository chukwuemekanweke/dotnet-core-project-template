using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed record VerifyTwoFactorEnrollmentCommand(string Code, ActorContext ActorContext);
