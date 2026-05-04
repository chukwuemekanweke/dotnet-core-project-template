using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class ProviderByIdSpecification : Specification<Provider>
{
    public ProviderByIdSpecification(Guid providerId)
    {
        Where(provider => provider.Id == providerId);
    }
}
