using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class PaymentTransactionByMerchantReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByMerchantReferenceSpecification(string merchantReference)
    {
        Where(transaction => transaction.MerchantReference == merchantReference);
        EnableTracking();
    }
}
