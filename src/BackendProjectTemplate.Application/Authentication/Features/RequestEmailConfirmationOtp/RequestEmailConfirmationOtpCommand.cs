using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed record RequestEmailConfirmationOtpCommand(string Email, ActorContext ActorContext);
