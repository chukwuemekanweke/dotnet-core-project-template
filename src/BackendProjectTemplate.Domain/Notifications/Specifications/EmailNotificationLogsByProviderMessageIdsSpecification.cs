using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogsByProviderMessageIdsSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogsByProviderMessageIdsSpecification(IReadOnlyCollection<string> providerMessageIds)
    {
        Where(log => log.ProviderMessageId != null && providerMessageIds.Contains(log.ProviderMessageId));
    }
}
