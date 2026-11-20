namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookResult(
    MailtrapDeliveryWebhookReceiptStatus Status,
    string? StatusChangeReason = null);
