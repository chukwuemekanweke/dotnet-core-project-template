namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record CompletePasswordResetCommand(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword,
    ActorContext ActorContext);
