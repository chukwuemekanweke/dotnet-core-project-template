using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class PaymentWebhookInboxByEventIdSpecification : Specification<PaymentWebhookInbox>
{
    public PaymentWebhookInboxByEventIdSpecification(Guid paymentProviderId, string webhookEventId)
    {
        Where(inbox =>
            inbox.PaymentProviderId == paymentProviderId &&
            inbox.WebhookEventId == webhookEventId);
    }
}
