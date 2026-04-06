using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSignInSuccessfulHandler(
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationIdentityService identityService,
    IAuthenticationNotificationSender notificationSender) : BaseMessageHandler<UserSignInSuccessful>(customTelemetryContext)
{
    protected override async Task HandleAsyncInternal(UserSignInSuccessful message, CancellationToken cancellationToken)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new CannotProcessMessageNonTransientException("UserSignInSuccessful must contain a non-empty UserId.");
        }

        var user = await identityService.FindByIdAsync(message.UserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSignInSuccessful because user '{message.UserId}' could not be found.");
        }

        var resetResult = await identityService.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to reset access-failed count for user {user.Id}.");
        }

        await notificationSender.SendSignInSuccessfulAsync(user, message.IpAddress, message.UserAgent, cancellationToken);
        CustomTelemetryContext.AddCustomEvent(Observability.UserSignInSuccessfulEventName, new Dictionary<string, string>
        {
            [Observability.UserIdPropertyName] = user.Id.ToString()
        });
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInSuccessful message)
    {
        yield return (Observability.UserIdPropertyName, message.UserId.ToString());
    }
}
