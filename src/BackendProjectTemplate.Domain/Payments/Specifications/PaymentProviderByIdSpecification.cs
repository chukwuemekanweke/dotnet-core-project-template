using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class PaymentProviderByIdSpecification : Specification<PaymentProvider>
{
    public PaymentProviderByIdSpecification(Guid paymentProviderId)
    {
        Where(provider => provider.Id == paymentProviderId);
        EnableTracking();
    }
}
