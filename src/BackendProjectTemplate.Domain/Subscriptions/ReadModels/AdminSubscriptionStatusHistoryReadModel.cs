namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionStatusHistoryReadModel(
    Guid HistoryId,
    SubscriptionStatus? PreviousStatus,
    SubscriptionStatus NewStatus,
    string Reason,
    string? Details,
    string? ActorId,
    Guid OperationId,
    Guid? PaymentTransactionId,
    DateTimeOffset OccurredAtUtc);
