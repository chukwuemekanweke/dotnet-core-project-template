namespace BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;

public sealed record RetrySubscriptionPurchaseFulfillmentResult(
    RetrySubscriptionPurchaseFulfillmentStatus Status,
    Guid? PurchaseId,
    Guid? PaymentTransactionId,
    string? Error);
