using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSignInFailedHandler(
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationIdentityService identityService,
    IAuthenticationNotificationSender notificationSender,
    ILogger<UserSignInFailedHandler> logger) : BaseMessageHandler<UserSignInFailed>(customTelemetryContext)
{
    protected override async Task HandleAsyncInternal(UserSignInFailed message, CancellationToken cancellationToken)
    {
        if (!message.UserId.HasValue)
        {
            logger.LogInformation(
                "Skipping sign-in failure processing because no user could be resolved for email {EmailAddress}.",
                message.EmailAddress);

            return;
        }

        var user = await identityService.FindByIdAsync(message.UserId.Value);
        if (user is null)
        {
            logger.LogWarning(
                "Unable to process sign-in failure for user {UserId} because the account could not be found.",
                message.UserId.Value);

            return;
        }

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials)
        {
            var accessFailedResult = await identityService.AccessFailedAsync(user);
            if (!accessFailedResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to increment access-failed count for user {user.Id}.");
            }
        }

        var properties = new Dictionary<string, string>
        {
            [Observability.UserIdPropertyName] = user.Id.ToString(),
            ["FailureReason"] = message.FailureReason
        };

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials &&
            await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user)
                ?? throw new InvalidOperationException($"User {user.Id} is locked out but has no lockout end time.");

            await notificationSender.SendAccountLockedAsync(user, lockedUntilUtc, cancellationToken);
            properties["LockedUntilUtc"] = lockedUntilUtc.ToString("O");
        }

        CustomTelemetryContext.AddCustomEvent(Observability.UserSignInFailedEventName, properties);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInFailed message)
    {
        if (message.UserId.HasValue)
        {
            yield return (Observability.UserIdPropertyName, message.UserId.Value.ToString());
        }
    }
}
