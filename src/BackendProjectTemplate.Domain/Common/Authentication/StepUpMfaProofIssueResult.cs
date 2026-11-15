namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record StepUpMfaProofIssueResult(
    string AuthorizationToken,
    DateTimeOffset ExpiresAtUtc);
