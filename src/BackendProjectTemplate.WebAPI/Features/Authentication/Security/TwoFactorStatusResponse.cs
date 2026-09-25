namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record TwoFactorStatusResponse(bool Enabled, int RecoveryCodesRemaining);
