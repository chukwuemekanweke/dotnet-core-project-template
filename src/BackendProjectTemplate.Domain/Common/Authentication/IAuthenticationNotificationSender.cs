using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IAuthenticationNotificationSender
{
    Task SendSignInSuccessfulAsync(AppUser user, string ipAddress, string userAgent, CancellationToken cancellationToken);
    Task SendAccountLockedAsync(AppUser user, DateTimeOffset lockedUntilUtc, CancellationToken cancellationToken);
}
