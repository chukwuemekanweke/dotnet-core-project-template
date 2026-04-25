using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class PendingPaymentReconciliationSpecification : Specification<PaymentTransaction>
{
    public PendingPaymentReconciliationSpecification(DateTimeOffset staleThresholdUtc, DateTimeOffset nextCheckThresholdUtc, int batchSize)
    {
        Where(transaction =>
            transaction.PaymentStatus == PaymentStatus.Initiated &&
            transaction.CreatedAtUtc <= staleThresholdUtc &&
            (transaction.LastStatusCheckAtUtc == null || transaction.LastStatusCheckAtUtc <= nextCheckThresholdUtc));
        ApplyOrderBy(transaction => transaction.CreatedAtUtc);
        ApplyPaging(0, batchSize);
        EnableTracking();
    }
}
