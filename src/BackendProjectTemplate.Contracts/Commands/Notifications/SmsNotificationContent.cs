namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record SmsNotificationContent(
    string CallingCode,
    string PhoneNumber,
    Dictionary<string, string> Content) : NotificationContent(Content);
