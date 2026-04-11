namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record WebPushNotificationContent(
    string Recipient,
    Dictionary<string, string> Content) : NotificationContent(Content);
