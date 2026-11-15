namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailRenewalResponse(
    Guid RenewalId,
    DateTimeOffset TargetPeriodStartUtc,
    DateTimeOffset TargetPeriodEndUtc,
    decimal ExpectedPrice,
    Guid CurrencyId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? NextRetryAtUtc,
    IReadOnlyList<SubscriptionDetailRenewalAttemptResponse> Attempts);
