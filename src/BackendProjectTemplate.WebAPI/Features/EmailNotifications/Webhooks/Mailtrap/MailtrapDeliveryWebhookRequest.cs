using System.Text.Json.Serialization;

namespace BackendProjectTemplate.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed record MailtrapDeliveryWebhookRequest(
    [property: JsonPropertyName("events")] 
    MailtrapDeliveryWebhookEventRequest[] Events);
