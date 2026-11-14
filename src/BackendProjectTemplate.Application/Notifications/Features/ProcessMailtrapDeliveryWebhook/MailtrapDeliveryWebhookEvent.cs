namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record MailtrapDeliveryWebhookEvent(
    string Event,
    string MessageId,
    string SendingStream,
    string Email,
    string SendingDomainName,
    long Timestamp,
    string EventId,
    string? Reason = null,
    string? Response = null,
    int? ResponseCode = null,
    string? BounceCategory = null);
