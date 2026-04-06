namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record EmailNotificationContent(
    string To,
    string[] Content,
    string[]? Cc = null,
    string[]? Bcc = null) : NotificationContent(Content);
