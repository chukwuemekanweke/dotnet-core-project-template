using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionStatusHistory : Entity
{
    public const int MaxReasonLength = 200;
    public const int MaxActorIdLength = 200;

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
        Guid? paymentTransactionId)
    {
        SubscriptionId = subscriptionId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason.Trim();
        OccurredAtUtc = occurredAtUtc;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        OperationId = operationId;
        PaymentTransactionId = paymentTransactionId;
    }

    public Guid SubscriptionId { get; private set; }
    public SubscriptionStatus? PreviousStatus { get; private set; }
    public SubscriptionStatus NewStatus { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? ActorId { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
}
