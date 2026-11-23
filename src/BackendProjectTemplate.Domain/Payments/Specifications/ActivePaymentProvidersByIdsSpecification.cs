using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class ActivePaymentProvidersByIdsSpecification : Specification<PaymentProvider>
{
    public ActivePaymentProvidersByIdsSpecification(IReadOnlyCollection<Guid> paymentProviderIds) =>
        Where(provider => paymentProviderIds.Contains(provider.Id) && provider.IsActive);
}
