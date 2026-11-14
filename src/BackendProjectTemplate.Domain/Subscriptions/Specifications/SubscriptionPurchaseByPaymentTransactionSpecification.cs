using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionPurchaseByPaymentTransactionSpecification : Specification<SubscriptionPurchase>
{
    public SubscriptionPurchaseByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(purchase => purchase.PaymentTransactionId == paymentTransactionId);
        EnableTracking();
    }
}
