using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Specifications;

public sealed class SubscriptionRenewalByPaymentTransactionSpecification : Specification<SubscriptionRenewal>
{
    public SubscriptionRenewalByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(renewal => renewal.Attempts.Any(attempt => attempt.PaymentTransactionId == paymentTransactionId));
        AddInclude(renewal => renewal.Attempts);
        EnableTracking();
    }
}
