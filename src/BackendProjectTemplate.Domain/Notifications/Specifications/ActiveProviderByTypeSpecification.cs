using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class ActiveProviderByTypeSpecification : Specification<Provider>
{
    public ActiveProviderByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType && provider.IsActive);
    }
}
