namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionRenewalAttemptReadModel(
    Guid AttemptId,
    int AttemptNumber,
    Guid? PaymentTransactionId,
    SubscriptionRenewalAttemptStatus Status,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
