using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class ActiveEmailProviderSpecification : Specification<EmailProvider>
{
    public ActiveEmailProviderSpecification()
    {
        Where(provider => provider.IsActive);
    }
}
