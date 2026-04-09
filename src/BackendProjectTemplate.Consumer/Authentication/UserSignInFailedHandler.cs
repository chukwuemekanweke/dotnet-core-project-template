using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSignInFailedHandler(
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
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
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSignInFailed because user '{message.UserId.Value}' could not be found.");
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
            var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(message.UserId.Value, cancellationToken);
            if (stakeholder is null)
            {
                throw new CannotProcessMessageNonTransientException(
                    $"Unable to process UserSignInFailed because no stakeholder could be found for user '{message.UserId.Value}'.");
            }

            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user)
                ?? throw new InvalidOperationException($"User {user.Id} is locked out but has no lockout end time.");

            await commandSender.SendAsync(
                new SendNotificationCommand(
                    stakeholder.TenantId,
                    stakeholder.CountryId,
                    NotificationType.AccountLocked,
                    NotificationMedium.Email,
                    new EmailNotificationContent(
                        user.Email ?? message.EmailAddress,
                        [
                            "Your account has been locked due to multiple failed sign-in attempts.",
                            $"Locked Until: {lockedUntilUtc:O}"
                        ])),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            properties["LockedUntilUtc"] = lockedUntilUtc.ToString("O");
        }

        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInFailed, properties);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInFailed message)
    {
        if (message.UserId.HasValue)
        {
            yield return (Observability.UserIdPropertyName, message.UserId.Value.ToString());
        }
    }
}
