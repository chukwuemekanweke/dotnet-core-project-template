namespace BackendProjectTemplate.Contracts.Commands.Subscriptions;

public sealed record ReconcileSubscriptionRenewalCommand(
    Guid RenewalId,
    Guid RenewalAttemptId) : BaseCommand;
