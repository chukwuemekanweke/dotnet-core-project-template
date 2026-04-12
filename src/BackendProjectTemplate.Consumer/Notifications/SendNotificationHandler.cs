using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Notifications;

public sealed class SendNotificationHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IEmailNotificationService emailNotificationService) : BaseMessageHandler<SendNotificationCommand>(customTelemetryContext, currentActorAccessor)
{
    protected override async Task HandleAsyncInternal(SendNotificationCommand message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message.NotificationMedium)
            {
                case NotificationMedium.Email:
                    await emailNotificationService.SendAsync(message, cancellationToken);
                    break;

                default:
                    throw new FailedToProcessMessageException(
                        $"Notification medium '{message.NotificationMedium}' is not supported.");
            }
        }
        catch (NotificationConfigurationException exception)
        {
            throw new CannotProcessMessageNonTransientException(exception.Message);
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Notifications.EmailSent,
            new Dictionary<string, string>
            {
                ["NotificationType"] = message.NotificationType.ToString(),
                ["NotificationMedium"] = message.NotificationMedium.ToString(),
                ["TenantId"] = message.TenantId.ToString(),
                ["CountryId"] = message.CountryId.ToString()
            });
    }
}
