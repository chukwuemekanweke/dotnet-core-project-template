using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class WalletTransactionByPaymentTransactionSpecification : Specification<WalletTransaction>
{
    public WalletTransactionByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(transaction => transaction.PaymentTransactionId == paymentTransactionId);
    }
}
