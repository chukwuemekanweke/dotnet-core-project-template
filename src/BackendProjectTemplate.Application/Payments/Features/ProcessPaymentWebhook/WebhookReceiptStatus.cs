namespace BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;

public enum WebhookReceiptStatus
{
    Persisted = 1,
    InvalidSignature = 2,
    UnidentifiedTransaction = 3,
    Duplicate = 4
}
