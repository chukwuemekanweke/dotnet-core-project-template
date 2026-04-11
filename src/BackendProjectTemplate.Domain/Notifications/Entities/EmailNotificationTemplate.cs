using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class EmailNotificationTemplate : Entity
{
    private EmailNotificationTemplate()
    {
    }

    private EmailNotificationTemplate(
        NotificationType notificationType,
        string description,
        string subject,
        string body,
        DateTimeOffset utcNow)
    {
        NotificationType = notificationType;
        Description = description.Trim();
        Subject = subject.Trim();
        Body = body.Trim();
        SetAuditDates(utcNow);
    }

    public NotificationType NotificationType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    public static EmailNotificationTemplate Create(
        NotificationType notificationType,
        string description,
        string subject,
        string body,
        DateTimeOffset utcNow) =>
        new(notificationType, description, subject, body, utcNow);
}
