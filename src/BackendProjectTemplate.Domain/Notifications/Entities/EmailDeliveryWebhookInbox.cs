using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class EmailDeliveryWebhookInbox : Entity, IAggregateRoot
{
    private EmailDeliveryWebhookInbox()
    {
    }

    private EmailDeliveryWebhookInbox(
        Guid providerId,
        string webhookEventId,
        string providerMessageId,
        string recipientEmail,
        string sendingStream,
        string sendingDomainName,
        string rawPayload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc)
    {
        ProviderId = providerId;
        WebhookEventId = webhookEventId;
        ProviderMessageId = providerMessageId;
        RecipientEmail = recipientEmail;
        SendingStream = sendingStream;
        SendingDomainName = sendingDomainName;
        RawPayload = rawPayload;
        OccurredAtUtc = occurredAtUtc;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid ProviderId { get; private set; }
    public string WebhookEventId { get; private set; } = string.Empty;
    public string ProviderMessageId { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string SendingStream { get; private set; } = string.Empty;
    public string SendingDomainName { get; private set; } = string.Empty;
    public string RawPayload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static EmailDeliveryWebhookInbox Create(
        Guid providerId,
        string webhookEventId,
        string providerMessageId,
        string recipientEmail,
        string sendingStream,
        string sendingDomainName,
        string rawPayload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc) =>
        new(
            providerId,
            NormalizeRequired(webhookEventId),
            NormalizeRequired(providerMessageId),
            NormalizeRequired(recipientEmail),
            NormalizeRequired(sendingStream),
            NormalizeRequired(sendingDomainName),
            NormalizeRequired(rawPayload),
            occurredAtUtc,
            receivedAtUtc);

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim();
    }
}
