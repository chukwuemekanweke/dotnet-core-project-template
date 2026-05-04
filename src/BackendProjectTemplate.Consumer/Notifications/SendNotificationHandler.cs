using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Notifications;

public sealed class SendNotificationHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IEmailNotificationService emailNotificationService) : BaseMessageHandler<SendNotificationCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SendNotificationCommand message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message.NotificationMedium)
            {
                case NotificationMedium.Email:
                    var sendResult = await emailNotificationService.SendAsync(message, cancellationToken);
                    if (sendResult is not null)
                    {
                        CustomTelemetryContext.AddCustomEvent(
                            Observability.EventNames.Notifications.EmailSent,
                            ObservabilityEventProperties.Create(
                                CurrentActorAccessor,
                                message.StakeholderId,
                                additionalProperties: new Dictionary<string, string>
                                {
                                    [Observability.MessageIdPropertyName] = message.MessageId.ToString(),
                                    [Observability.ProviderKeyPropertyName] = sendResult.ProviderKey,
                                    [Observability.ProviderMessageIdPropertyName] = sendResult.ProviderMessageId,
                                    [Observability.NotificationTypePropertyName] = message.NotificationType.ToString()
                                }));
                    }
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
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(SendNotificationCommand message)
    {
        yield break;
    }
}
