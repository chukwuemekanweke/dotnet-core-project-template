namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

using BackendProjectTemplate.Domain.Common.Authentication;

public sealed record GoogleSignUpResult(
    GoogleSignUpStatus Status,
    string? Email = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? RetryAtUtc = null);
