using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class TestOtpDeliveryService : IOtpDeliveryService
{
    private readonly Dictionary<string, string> _codes = new(StringComparer.OrdinalIgnoreCase);

    public Task SendSignUpOtpAsync(AppUser user, string otpCode, CancellationToken cancellationToken)
    {
        _codes[user.Email ?? throw new InvalidOperationException("The user email is required for OTP delivery.")] = otpCode;
        return Task.CompletedTask;
    }

    public string? GetCode(string email) =>
        _codes.TryGetValue(email, out var code) ? code : null;
}
