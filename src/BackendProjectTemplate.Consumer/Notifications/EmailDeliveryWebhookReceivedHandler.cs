using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Notifications;

public sealed class EmailDeliveryWebhookReceivedHandler(
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IMessageHandler<EmailDeliveryWebhookReceived>
{
    public async Task HandleAsync(EmailDeliveryWebhookReceived message, CancellationToken cancellationToken)
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

        var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
            new EmailNotificationLogByProviderMessageIdSpecification(message.ProviderMessageId),
            cancellationToken);
        if (emailNotificationLog is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no notification log was found for provider message '{message.ProviderMessageId}'.");
        }

        var inbox = await emailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
            new EmailDeliveryWebhookInboxByEventIdSpecification(message.ProviderId, message.EventId),
            cancellationToken);
        if (inbox is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no webhook inbox was found for event '{message.EventId}'.");
        }

        var now = timeProvider.GetUtcNow();
        emailNotificationLog.MarkDelivered(inbox.OccurredAtUtc, now);
        emailNotificationLogRepository.Update(emailNotificationLog);
        inbox.MarkProcessed("notification_log_delivered", now);
        emailDeliveryWebhookInboxRepository.Update(inbox);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
