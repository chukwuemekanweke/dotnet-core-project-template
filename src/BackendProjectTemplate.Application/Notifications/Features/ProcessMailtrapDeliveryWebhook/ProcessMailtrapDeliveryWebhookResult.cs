namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus Status);

public enum MailtrapDeliveryWebhookReceiptStatus
{
    Persisted = 0,
    Duplicate = 1,
    InvalidSignature = 2
}
