namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record ActivateAllocatedSubscriptionCommand(
    Guid SubscriptionId,
    Guid AllocationId,
    DateTimeOffset ExpectedStartUtc) : BaseCommand;
