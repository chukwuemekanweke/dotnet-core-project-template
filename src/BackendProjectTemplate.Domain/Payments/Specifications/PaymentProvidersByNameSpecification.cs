using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

/// <summary>
/// Every visible (non-soft-deleted) payment provider, active or inactive, ordered by name with the
/// identifier as a stable tie-breaker. Configurations are intentionally not included.
/// </summary>
public sealed class PaymentProvidersByNameSpecification : Specification<PaymentProvider>
{
    public PaymentProvidersByNameSpecification()
    {
        ApplyOrderBy(provider => provider.ProviderName);
        ApplyThenBy(provider => provider.Id);
    }
}
