namespace BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;

public static class BachsWebhookEvents
{
    public const string CollectionSucceeded = "collection.succeeded";
    public const string CollectionFailed = "collection.failed";
    public const string CheckoutExpired = "checkout.expired";
}
