namespace BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;

public sealed record VerifyStepUpMfaResult(
    VerifyStepUpMfaStatus Status,
    string? AuthorizationToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    DateTimeOffset? LockedUntilUtc = null);
