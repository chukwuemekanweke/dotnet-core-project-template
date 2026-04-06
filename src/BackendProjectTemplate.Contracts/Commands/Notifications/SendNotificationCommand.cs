namespace BackendProjectTemplate.Contracts.Commands.Notifications;

public sealed record SendNotificationCommand(
    Guid TenantId,
    Guid CountryId,
    NotificationType NotificationType,
    NotificationMedium NotificationMedium,
    NotificationContent NotificationContent) : BaseCommand;
