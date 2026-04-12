using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Notifications.Entities;

public sealed class EmailNotificationLog : Entity
{
    private const int MaxFailureReasonLength = 4000;

    private EmailNotificationLog()
    {
    }

    private EmailNotificationLog(
        Guid messageId,
        string to,
        string? cc,
        string? bcc)
    {
        MessageId = messageId;
        To = to.Trim();
        Cc = string.IsNullOrWhiteSpace(cc) ? null : cc.Trim();
        Bcc = string.IsNullOrWhiteSpace(bcc) ? null : bcc.Trim();
        IsSent = false;
    }

    public Guid MessageId { get; private set; }
    public string To { get; private set; } = string.Empty;
    public string? Cc { get; private set; }
    public string? Bcc { get; private set; }
    public bool IsSent { get; private set; }
    public string? FailureReason { get; private set; }

    public static EmailNotificationLog Create(
        Guid messageId,
        string to,
        string? cc,
        string? bcc) =>
        new(messageId, to, cc, bcc);

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
