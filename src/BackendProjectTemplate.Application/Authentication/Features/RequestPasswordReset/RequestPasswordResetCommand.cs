namespace BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record RequestPasswordResetCommand(string Email, ActorContext ActorContext);
