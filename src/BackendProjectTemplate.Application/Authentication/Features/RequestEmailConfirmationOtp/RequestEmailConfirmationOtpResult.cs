namespace BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed record RequestEmailConfirmationOtpResult(
    RequestEmailConfirmationOtpStatus Status,
    DateTimeOffset RetryAtUtc);

public enum RequestEmailConfirmationOtpStatus
{
    Accepted = 1,
    UserNotFound = 2,
    AlreadyVerified = 3
}
