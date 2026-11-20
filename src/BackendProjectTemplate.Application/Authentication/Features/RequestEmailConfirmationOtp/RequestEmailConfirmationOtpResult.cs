namespace BackendProjectTemplate.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed record RequestEmailConfirmationOtpResult(
    RequestEmailConfirmationOtpStatus Status,
    DateTimeOffset RetryAtUtc);
