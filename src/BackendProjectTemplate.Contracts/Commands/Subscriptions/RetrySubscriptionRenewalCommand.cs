namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record RetrySubscriptionRenewalCommand(
    Guid RenewalId,
    int ExpectedAttemptNumber) : BaseCommand;
