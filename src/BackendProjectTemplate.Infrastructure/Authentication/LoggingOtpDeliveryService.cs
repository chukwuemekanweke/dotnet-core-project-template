using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Identity.Entities;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class LoggingOtpDeliveryService(ILogger<LoggingOtpDeliveryService> logger) : IOtpDeliveryService
{
    public Task SendSignUpOtpAsync(AppUser user, string otpCode, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development OTP for {Email}: {OtpCode}", user.Email, otpCode);
        return Task.CompletedTask;
    }
}
