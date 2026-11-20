namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed record SignUpResult(
    SignUpStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    DateTimeOffset? RetryAtUtc = null);
