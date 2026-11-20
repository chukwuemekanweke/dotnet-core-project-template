namespace BackendProjectTemplate.Application.Authentication.Features.GetTwoFactorStatus;

public sealed record GetTwoFactorStatusResult(
    GetTwoFactorStatusStatus Status,
    bool Enabled = false,
    int RecoveryCodesRemaining = 0);
