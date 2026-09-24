namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record GoogleAuthenticationFlowResult(
    GoogleAuthenticationFlowStatus Status,
    GoogleAuthenticationFlow? Flow = null);
