namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus Status);

public enum MailtrapDeliveryWebhookReceiptStatus
{
    Persisted = 1,
    Duplicate = 2,
    InvalidSignature = 3
}
