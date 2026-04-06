namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record SmsNotificationContent(
    string CallingCode,
    string PhoneNumber,
    string[] Content) : NotificationContent(Content);
