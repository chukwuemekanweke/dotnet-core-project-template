namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookCommand(
    IReadOnlyCollection<MailtrapDeliveryWebhookEvent> Events,
    string RawPayload,
    byte[] RawPayloadBytes,
    string? SignatureHeader)
{
    public ProcessMailtrapDeliveryWebhookCommand(
        IReadOnlyCollection<MailtrapDeliveryWebhookEvent> events,
        string rawPayload,
        string? signatureHeader)
        : this(events, rawPayload, System.Text.Encoding.UTF8.GetBytes(rawPayload), signatureHeader)
    {
    }
}
