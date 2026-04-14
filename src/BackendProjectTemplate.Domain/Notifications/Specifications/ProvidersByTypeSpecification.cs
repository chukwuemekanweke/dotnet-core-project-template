using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class ProvidersByTypeSpecification : Specification<Provider>
{
    public ProvidersByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType);
    }
}
