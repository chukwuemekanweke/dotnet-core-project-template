namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailStatusHistoryResponse(
    Guid HistoryId,
    string? PreviousStatus,
    string NewStatus,
    string Reason,
    string? Details,
    string? ActorId,
    Guid OperationId,
    Guid? PaymentTransactionId,
    DateTimeOffset OccurredAtUtc);
