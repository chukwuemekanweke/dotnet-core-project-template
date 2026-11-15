namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionAllocationReadModel(
    Guid AllocationId,
    SubscriptionAllocationSource AllocationSource,
    string Reason,
    string AllocatedByActorId,
    Guid OperationId,
    Guid PlanId,
    string PlanCode,
    decimal NominalPrice,
    Guid CurrencyId,
    BillingInterval BillingInterval,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    DateTimeOffset CreatedAtUtc);
