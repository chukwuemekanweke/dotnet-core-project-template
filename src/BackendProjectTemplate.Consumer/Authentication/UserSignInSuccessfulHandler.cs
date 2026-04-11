using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSignInSuccessfulHandler(
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork) : BaseMessageHandler<UserSignInSuccessful>(customTelemetryContext)
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

        var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(message.UserId, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSignInSuccessful because no stakeholder could be found for user '{message.UserId}'.");
        }

        var resetResult = await identityService.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to reset access-failed count for user {user.Id}.");
        }

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.TenantId,
                stakeholder.CountryId,
                NotificationType.SignInSuccessful,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    user.Email ?? message.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["IpAddress"] = message.IpAddress,
                        ["UserAgent"] = message.UserAgent
                    })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserSignInSuccessful, new Dictionary<string, string>
        {
            [Observability.UserIdPropertyName] = user.Id.ToString()
        });
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInSuccessful message)
    {
        yield return (Observability.UserIdPropertyName, message.UserId.ToString());
    }
}
