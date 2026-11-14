namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;

public sealed record CancelSubscriptionAtPeriodEndResult(
    bool Found,
    bool Changed,
    DateTimeOffset? EntitledUntilUtc);
