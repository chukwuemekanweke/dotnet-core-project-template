namespace BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;

public enum RetrySubscriptionPurchaseFulfillmentStatus
{
    Requested = 1,
    NotFound = 2,
    NotBlocked = 3,
    PaymentNotSucceeded = 4
}
