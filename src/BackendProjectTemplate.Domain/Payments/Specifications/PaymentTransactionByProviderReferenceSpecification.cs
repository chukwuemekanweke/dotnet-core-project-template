using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class PaymentTransactionByProviderReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByProviderReferenceSpecification(string providerReference)
    {
        Where(transaction => transaction.ProviderReference == providerReference);
        EnableTracking();
    }
}
