namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record AppPushNotificationContent(
    string Recipient,
    Dictionary<string, string> Content) : NotificationContent(Content);
