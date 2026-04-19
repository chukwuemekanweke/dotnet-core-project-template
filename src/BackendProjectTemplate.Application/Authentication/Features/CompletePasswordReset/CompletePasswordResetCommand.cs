namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

public sealed record CompletePasswordResetCommand(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword);
