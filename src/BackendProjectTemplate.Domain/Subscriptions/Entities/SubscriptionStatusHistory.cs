using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionStatusHistory : Entity
{
    public const int MaxReasonLength = 200;
    public const int MaxActorIdLength = 200;
    public const int MaxDetailsLength = 1000;

    private SubscriptionStatusHistory()
    {
    }

    internal SubscriptionStatusHistory(
        Guid subscriptionId,
        SubscriptionStatus? previousStatus,
        SubscriptionStatus newStatus,
        string reason,
        DateTimeOffset occurredAtUtc,
        string? actorId,
        Guid operationId,
        Guid? paymentTransactionId,
        string? details = null)
    {
        SubscriptionId = subscriptionId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason.Trim();
        OccurredAtUtc = occurredAtUtc;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        OperationId = operationId;
        PaymentTransactionId = paymentTransactionId;
        Details = NormalizeDetails(details);
    }

    public Guid SubscriptionId { get; private set; }
    public SubscriptionStatus? PreviousStatus { get; private set; }
    public SubscriptionStatus NewStatus { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? ActorId { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Free-text business justification supplied by an administrator for this transition. Lifecycle
    /// <see cref="Reason"/> remains a stable machine-readable code.
    /// </summary>
    public string? Details { get; private set; }

    private static string? NormalizeDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        var normalized = details.Trim();
        return normalized.Length <= MaxDetailsLength
            ? normalized
            : throw new ArgumentException($"Details must not exceed {MaxDetailsLength} characters.", nameof(details));
    }
}
