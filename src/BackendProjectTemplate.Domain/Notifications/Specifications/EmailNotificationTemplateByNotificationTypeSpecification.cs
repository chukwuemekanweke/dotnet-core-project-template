using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Specifications;

public sealed class EmailNotificationTemplateByNotificationTypeSpecification : Specification<EmailNotificationTemplate>
{
    public EmailNotificationTemplateByNotificationTypeSpecification(NotificationType notificationType)
    {
        Where(template => template.NotificationType == notificationType);
    }
}
