namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public static class MailtrapDeliveryWebhookEvents
{
    public const string Delivery = "delivery";
    public const string Bounce = "bounce";
    public const string Reject = "reject";

    public static bool IsSupported(string eventType) =>
        eventType is Delivery or Bounce or Reject;
}
