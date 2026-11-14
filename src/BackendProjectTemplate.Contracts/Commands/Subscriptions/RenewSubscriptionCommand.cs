namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record RenewSubscriptionCommand(
    Guid SubscriptionId,
    DateTimeOffset TargetPeriodStartUtc) : BaseCommand;
