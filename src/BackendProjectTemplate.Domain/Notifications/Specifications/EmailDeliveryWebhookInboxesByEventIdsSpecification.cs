using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxesByEventIdsSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxesByEventIdsSpecification(Guid providerId, IReadOnlyCollection<string> webhookEventIds)
    {
        Where(inbox => inbox.ProviderId == providerId && webhookEventIds.Contains(inbox.WebhookEventId));
    }
}
