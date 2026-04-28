namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenTokenRequest(
    string GrantType,
    string ClientId,
    string ClientAssertion,
    string ClientAssertionType);
