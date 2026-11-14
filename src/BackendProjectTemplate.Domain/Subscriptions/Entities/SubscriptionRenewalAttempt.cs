using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionRenewalAttempt : Entity
{
    private SubscriptionRenewalAttempt()
    {
    }

    internal SubscriptionRenewalAttempt(
        Guid renewalId,
        int attemptNumber,
        Guid recurringAuthorizationId,
        string providerIdempotencyKey,
        DateTimeOffset createdAtUtc)
    {
        RenewalId = renewalId;
        AttemptNumber = attemptNumber;
        RecurringAuthorizationId = recurringAuthorizationId;
        ProviderIdempotencyKey = providerIdempotencyKey;
        Status = SubscriptionRenewalAttemptStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RenewalId { get; private set; }
    public int AttemptNumber { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public Guid RecurringAuthorizationId { get; private set; }
    public string ProviderIdempotencyKey { get; private set; } = string.Empty;
    public SubscriptionRenewalAttemptStatus Status { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
}
