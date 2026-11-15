namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailAllocationResponse(
    Guid AllocationId,
    string AllocationSource,
    string Reason,
    string AllocatedByActorId,
    Guid OperationId,
    Guid PlanId,
    string PlanCode,
    decimal NominalPrice,
    Guid CurrencyId,
    string BillingInterval,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    DateTimeOffset CreatedAtUtc);
