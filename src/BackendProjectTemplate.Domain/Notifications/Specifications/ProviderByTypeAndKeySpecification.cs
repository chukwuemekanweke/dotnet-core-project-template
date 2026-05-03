using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class ProviderByTypeAndKeySpecification : Specification<Provider>
{
    public ProviderByTypeAndKeySpecification(ProviderType providerType, string providerKey)
    {
        Where(provider =>
            provider.ProviderType == providerType &&
            provider.ProviderKey == providerKey);
    }
}
