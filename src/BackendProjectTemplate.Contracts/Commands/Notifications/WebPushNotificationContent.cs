namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record WebPushNotificationContent(
    string Recipient,
    string[] Content) : NotificationContent(Content);
