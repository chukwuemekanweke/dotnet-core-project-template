namespace BackendProjectTemplate.Application.Authentication.Features.SetupTwoFactor;

public sealed record SetupTwoFactorResult(
    SetupTwoFactorStatus Status,
    string? SharedKey = null,
    string? AuthenticatorUri = null);
