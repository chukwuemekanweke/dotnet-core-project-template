namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed record SignUpResult(
    SignUpStatus Status,
    DateTimeOffset? OtpExpiresAtUtc,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public enum SignUpStatus
{
    Accepted = 1,
    DuplicateEmail = 2,
    ValidationFailed = 3
}
