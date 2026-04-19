using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class EmailNotificationLog : Entity, IAggregateRoot
{
    private const int MaxFailureReasonLength = 4000;

    private EmailNotificationLog()
    {
    }

    private EmailNotificationLog(
        Guid messageId,
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        Dictionary<string, string> notificationContent,
        string to,
        string? cc,
        string? bcc)
    {
        MessageId = messageId;
        TenantId = tenantId;
        CountryId = countryId;
        NotificationType = notificationType;
        NotificationContent = new Dictionary<string, string>(notificationContent);
        To = to.Trim();
        Cc = string.IsNullOrWhiteSpace(cc) ? null : cc.Trim();
        Bcc = string.IsNullOrWhiteSpace(bcc) ? null : bcc.Trim();
        IsSent = false;
    }

    public Guid MessageId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CountryId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public Dictionary<string, string> NotificationContent { get; private set; } = [];
    public string To { get; private set; } = string.Empty;
    public string? Cc { get; private set; }
    public string? Bcc { get; private set; }
    public bool IsSent { get; private set; }
    public string? FailureReason { get; private set; }

    public static EmailNotificationLog Create(
        Guid messageId,
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        Dictionary<string, string> notificationContent,
        string to,
        string? cc,
        string? bcc) =>
        new(messageId, tenantId, countryId, notificationType, notificationContent, to, cc, bcc);

    public void MarkSent(DateTimeOffset utcNow)
    {
        IsSent = true;
        FailureReason = null;
        Touch(utcNow);
    }

    public void MarkFailed(string reason, DateTimeOffset utcNow)
    {
        IsSent = false;
        FailureReason = NormalizeFailureReason(reason);
        Touch(utcNow);
    }

    private static string NormalizeFailureReason(string reason)
    {
        var normalized = string.IsNullOrWhiteSpace(reason)
            ? "Unknown email delivery failure."
            : reason.Trim();

        return normalized.Length <= MaxFailureReasonLength
            ? normalized
            : normalized[..MaxFailureReasonLength];
    }
}
