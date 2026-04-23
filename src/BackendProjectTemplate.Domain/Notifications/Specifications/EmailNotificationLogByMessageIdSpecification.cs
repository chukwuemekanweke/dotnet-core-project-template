using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByMessageIdSpecification(Guid messageId)
    {
        Where(log => log.MessageId == messageId);
    }
}
