using System.Text.Json.Serialization;

namespace BackendProjectTemplate.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed record MailtrapDeliveryWebhookEventRequest(
    [property: JsonPropertyName("event")] 
    string Event,
    [property: JsonPropertyName("message_id")] 
    string MessageId,
    [property: JsonPropertyName("sending_stream")] 
    string SendingStream,
    [property: JsonPropertyName("email")] 
    string Email,
    [property: JsonPropertyName("sending_domain_name")] 
    string SendingDomainName,
    [property: JsonPropertyName("timestamp")] 
    long Timestamp,
    [property: JsonPropertyName("event_id")] 
    string EventId,
    [property: JsonPropertyName("reason")]
    string? Reason,
    [property: JsonPropertyName("response")]
    string? Response,
    [property: JsonPropertyName("response_code")]
    int? ResponseCode,
    [property: JsonPropertyName("bounce_category")]
    string? BounceCategory);
