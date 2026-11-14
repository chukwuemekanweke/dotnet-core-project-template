using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Notifications;

public sealed class EmailDeliveryWebhookReceivedHandler(
    IReadRepository<Provider> providerRepository,
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<EmailDeliveryWebhookReceivedHandler> logger) : BaseMessageHandler<EmailDeliveryWebhookReceived>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    protected override async Task HandleAsyncInternal(EmailDeliveryWebhookReceived message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ProviderMessageId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider message id.");
        }

        if (message.ProviderId == Guid.Empty)
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider id.");
        }

        if (string.IsNullOrWhiteSpace(message.EventId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid event id.");
        }

        var inbox = await emailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
            new EmailDeliveryWebhookInboxByEventIdSpecification(message.ProviderId, message.EventId),
            cancellationToken);
        if (inbox is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no webhook inbox was found for event '{message.EventId}'.");
        }

        if (inbox.WebhookProcessingStatus == WebhookProcessingStatus.Processed)
        {
            return;
        }

        var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
            new EmailNotificationLogByProviderMessageIdSpecification(message.ProviderMessageId),
            cancellationToken);
        if (emailNotificationLog is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no notification log was found for provider message '{message.ProviderMessageId}'.");
        }

        var now = Clock.GetUtcNow();
        var previousStatus = emailNotificationLog.DeliveryStatus;
        ApplyDeliveryOutcome(emailNotificationLog, inbox);
        emailNotificationLogRepository.Update(emailNotificationLog);
        inbox.MarkProcessed(GetStatusChangeReason(inbox.EventType), now);
        emailDeliveryWebhookInboxRepository.Update(inbox);

        if (emailNotificationLog.DeliveryStatus == previousStatus)
        {
            return;
        }

        var provider = await providerRepository.FirstOrDefaultAsync(
            new ProviderByIdSpecification(message.ProviderId),
            cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            GetObservabilityEventName(emailNotificationLog.DeliveryStatus),
            ObservabilityEventProperties.Create(
                ActorAccessor,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.MessageId] = emailNotificationLog.MessageId.ToString(),
                    [Observability.PropertyNames.Notifications.ProviderKey] = provider?.ProviderKey ?? string.Empty,
                    [Observability.PropertyNames.Notifications.ProviderMessageId] = emailNotificationLog.ProviderMessageId ?? message.ProviderMessageId,
                    [Observability.PropertyNames.Notifications.NotificationType] = emailNotificationLog.NotificationType.ToString()
                }));
    }

    private static void ApplyDeliveryOutcome(
        EmailNotificationLog emailNotificationLog,
        EmailDeliveryWebhookInbox inbox)
    {
        switch (inbox.EventType)
        {
            case MailtrapDeliveryWebhookEventTypes.Delivery:
                emailNotificationLog.MarkDelivered(inbox.OccurredAtUtc);
                break;
            case MailtrapDeliveryWebhookEventTypes.Bounce:
                emailNotificationLog.MarkBounced(inbox.OccurredAtUtc, inbox.FailureReason);
                break;
            case MailtrapDeliveryWebhookEventTypes.Reject:
                emailNotificationLog.MarkRejected(inbox.OccurredAtUtc, inbox.FailureReason);
                break;
            default:
                throw new CannotProcessMessageNonTransientException(
                    $"Unsupported Mailtrap delivery event type '{inbox.EventType}'.");
        }
    }

    private static string GetStatusChangeReason(string eventType) => eventType switch
    {
        MailtrapDeliveryWebhookEventTypes.Delivery => KnownWebhookStatusChangeReasons.Notifications.NotificationLogDelivered,
        MailtrapDeliveryWebhookEventTypes.Bounce => KnownWebhookStatusChangeReasons.Notifications.NotificationLogBounced,
        MailtrapDeliveryWebhookEventTypes.Reject => KnownWebhookStatusChangeReasons.Notifications.NotificationLogRejected,
        _ => throw new CannotProcessMessageNonTransientException($"Unsupported Mailtrap delivery event type '{eventType}'.")
    };

    private static string GetObservabilityEventName(EmailDeliveryStatus status) => status switch
    {
        EmailDeliveryStatus.Delivered => Observability.EventNames.Notifications.EmailDelivered,
        EmailDeliveryStatus.Bounced => Observability.EventNames.Notifications.EmailBounced,
        EmailDeliveryStatus.Rejected => Observability.EventNames.Notifications.EmailRejected,
        _ => throw new InvalidOperationException($"Email delivery status '{status}' is not terminal.")
    };

    private static class MailtrapDeliveryWebhookEventTypes
    {
        public const string Delivery = "delivery";
        public const string Bounce = "bounce";
        public const string Reject = "reject";
    }
}

