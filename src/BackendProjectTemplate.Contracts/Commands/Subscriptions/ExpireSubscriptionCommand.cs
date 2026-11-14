namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record ExpireSubscriptionCommand(
    Guid SubscriptionId,
    DateTimeOffset ExpectedPeriodEndUtc) : BaseCommand;
