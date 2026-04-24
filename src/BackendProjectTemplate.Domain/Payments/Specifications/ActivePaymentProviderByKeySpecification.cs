using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class ActivePaymentProviderByKeySpecification : Specification<PaymentProvider>
{
    public ActivePaymentProviderByKeySpecification(string providerKey)
    {
        Where(provider => provider.ProviderKey == providerKey && provider.IsActive);
    }
}
