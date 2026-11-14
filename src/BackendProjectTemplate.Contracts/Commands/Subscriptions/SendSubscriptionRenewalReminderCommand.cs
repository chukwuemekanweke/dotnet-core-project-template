namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record SendSubscriptionRenewalReminderCommand(
    Guid SubscriptionId,
    Guid RenewalId,
    int ReminderSequence) : BaseCommand;
