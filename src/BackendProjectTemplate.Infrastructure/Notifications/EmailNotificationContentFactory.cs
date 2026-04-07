using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal static class EmailNotificationContentFactory
{
    public static EmailDeliveryMessage Create(
        SendNotificationCommand command,
        EmailNotificationContent content,
        EmailNotificationsOptions options) =>
        new(
            options.FromAddress,
            options.FromName,
            content.To,
            content.Content,
            GetSubject(command.NotificationType),
            content.Cc,
            content.Bcc);

    private static string GetSubject(NotificationType notificationType) =>
        notificationType switch
        {
            NotificationType.AccountCreated => "Your account has been created",
            NotificationType.EmailConfirmationOtp => "Confirm your email address",
            NotificationType.ResetPasswordOtp => "Reset your password",
            NotificationType.PasswordResetSuccessful => "Your password has been reset",
            NotificationType.EmailConfirmationFollowUp => "Reminder to confirm your email",
            NotificationType.SignInSuccessful => "Successful sign-in",
            NotificationType.AccountLocked => "Your account has been locked",
            _ => throw new NotificationConfigurationException(
                $"Notification type '{notificationType}' is not mapped to an email subject.")
        };
}
