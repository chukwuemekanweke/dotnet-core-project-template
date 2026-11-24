using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

internal static class SubscriptionAllocationTestData
{
    public static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-15T10:00:00Z");
    public const string ActorId = "019a86a6-2eed-7a1d-a8f0-f78309f45899";
    public const string Reason = "Complimentary access for partner onboarding";

    public static SubscriptionPlan CreatePlan(BillingInterval interval = BillingInterval.Monthly, decimal price = 500000m) =>
        SubscriptionPlan.Create($"PLAN_{Guid.CreateVersion7():N}"[..20], "Partner Plan", price, Guid.CreateVersion7(), interval, true);

    public static SubscriptionAggregate Allocate(
        SubscriptionPlan plan,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? operationId = null) =>
        SubscriptionAggregate.CreateAllocated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            plan,
            plan.Prices.Single().BillingInterval,
            startUtc,
            endUtc,
            Now,
            operationId ?? Guid.CreateVersion7(),
            ActorId,
            Reason);
}
