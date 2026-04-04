namespace BackendProjectTemplate.Application.Identity.Features.SignUpOtp;

public sealed class SignUpOtpRequest
{
    public string Email { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}
