using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class LoggingAuthenticationNotificationSender(
    ILogger<LoggingAuthenticationNotificationSender> logger) : IAuthenticationNotificationSender
{
    public Task SendSignInSuccessfulAsync(AppUser user, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stub sign-in success email for {Email}. IpAddress: {IpAddress}. UserAgent: {UserAgent}",
            user.Email,
            ipAddress,
            userAgent);

        return Task.CompletedTask;
    }

    public Task SendAccountLockedAsync(AppUser user, DateTimeOffset lockedUntilUtc, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stub account locked email for {Email}. Locked until {LockedUntilUtc:O}",
            user.Email,
            lockedUntilUtc);

        return Task.CompletedTask;
    }
}
