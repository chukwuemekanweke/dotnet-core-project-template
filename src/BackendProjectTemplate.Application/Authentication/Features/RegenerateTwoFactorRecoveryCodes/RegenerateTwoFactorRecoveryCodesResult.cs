namespace BackendProjectTemplate.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed record RegenerateTwoFactorRecoveryCodesResult(
    RegenerateTwoFactorRecoveryCodesStatus Status,
    IReadOnlyList<string>? RecoveryCodes = null);
