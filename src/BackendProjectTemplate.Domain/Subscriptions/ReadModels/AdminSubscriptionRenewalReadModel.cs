namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionRenewalReadModel(
    Guid RenewalId,
    DateTimeOffset TargetPeriodStartUtc,
    DateTimeOffset TargetPeriodEndUtc,
    decimal ExpectedPrice,
    Guid CurrencyId,
    SubscriptionRenewalStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? NextRetryAtUtc,
    IReadOnlyList<AdminSubscriptionRenewalAttemptReadModel> Attempts);
