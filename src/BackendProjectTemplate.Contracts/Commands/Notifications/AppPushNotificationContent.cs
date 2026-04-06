namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record AppPushNotificationContent(
    string Recipient,
    string[] Content) : NotificationContent(Content);
