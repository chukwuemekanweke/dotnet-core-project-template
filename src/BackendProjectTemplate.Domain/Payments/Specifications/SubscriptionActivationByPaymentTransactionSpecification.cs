using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class SubscriptionActivationByPaymentTransactionSpecification : Specification<SubscriptionActivation>
{
    public SubscriptionActivationByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(activation => activation.PaymentTransactionId == paymentTransactionId);
    }
}
