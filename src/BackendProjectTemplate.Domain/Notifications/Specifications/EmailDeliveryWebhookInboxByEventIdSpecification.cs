using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxByEventIdSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxByEventIdSpecification(Guid providerId, string webhookEventId)
    {
        Where(inbox => inbox.ProviderId == providerId && inbox.WebhookEventId == webhookEventId);
    }
}
