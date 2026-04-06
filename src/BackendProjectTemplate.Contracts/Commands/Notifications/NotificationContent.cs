using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Contracts.Commands.Notifications;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "contentType")]
[JsonDerivedType(typeof(EmailNotificationContent), "email")]
[JsonDerivedType(typeof(SmsNotificationContent), "sms")]
[JsonDerivedType(typeof(WebPushNotificationContent), "webPush")]
[JsonDerivedType(typeof(AppPushNotificationContent), "appPush")]
public abstract record NotificationContent(string[] Content);
