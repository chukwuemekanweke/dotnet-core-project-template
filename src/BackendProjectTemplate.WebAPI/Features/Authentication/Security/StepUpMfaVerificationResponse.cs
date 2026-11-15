namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record StepUpMfaVerificationResponse(
    string AuthorizationToken,
    DateTimeOffset ExpiresAtUtc);
