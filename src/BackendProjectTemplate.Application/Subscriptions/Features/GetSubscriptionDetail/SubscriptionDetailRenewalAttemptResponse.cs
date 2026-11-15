namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailRenewalAttemptResponse(
    Guid AttemptId,
    int AttemptNumber,
    Guid? PaymentTransactionId,
    string Status,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
