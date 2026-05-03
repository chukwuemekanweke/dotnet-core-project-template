using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByProviderMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByProviderMessageIdSpecification(string providerMessageId)
    {
        Where(log => log.ProviderMessageId == providerMessageId);
    }
}
