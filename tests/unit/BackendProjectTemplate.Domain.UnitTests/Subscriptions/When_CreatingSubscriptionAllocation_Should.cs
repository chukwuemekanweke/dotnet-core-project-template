using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CreatingSubscriptionAllocation_Should
{
    [Fact]
    public void SnapshotPlanTermsAndAuditMetadata()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan(BillingInterval.Yearly, 1200000m);
        var operationId = Guid.CreateVersion7();
        var start = SubscriptionAllocationTestData.Now.AddDays(3);
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, start.AddYears(1), operationId);

        var allocation = SubscriptionAllocation.Create(
            subscription,
            plan,
            SubscriptionAllocationSource.Promotion,
            $"  {SubscriptionAllocationTestData.Reason}  ",
            SubscriptionAllocationTestData.ActorId,
            operationId, new string('f', 64));
        plan.UpdateTerms("Renamed", new Dictionary<BillingInterval, decimal> { [BillingInterval.Yearly] = 1500000m }, null, []);

        allocation.SubscriptionId.ShouldBe(subscription.Id);
        allocation.TenantId.ShouldBe(subscription.TenantId);
        allocation.BillingAccountId.ShouldBe(subscription.BillingAccountId);
        allocation.PlanId.ShouldBe(plan.Id);
        allocation.PlanCode.ShouldBe(plan.Code);
        allocation.NominalPrice.ShouldBe(1200000m);
        allocation.CurrencyId.ShouldBe(plan.CurrencyId);
        allocation.BillingInterval.ShouldBe(BillingInterval.Yearly);
        allocation.StartAtUtc.ShouldBe(start);
        allocation.EndAtUtc.ShouldBe(start.AddYears(1));
        allocation.AllocationSource.ShouldBe(SubscriptionAllocationSource.Promotion);
        allocation.Reason.ShouldBe(SubscriptionAllocationTestData.Reason);
        allocation.AllocatedByActorId.ShouldBe(SubscriptionAllocationTestData.ActorId);
        allocation.OperationId.ShouldBe(operationId);
        Should.Throw<ArgumentOutOfRangeException>(() => SubscriptionAllocation.Create(
            subscription, plan, (SubscriptionAllocationSource)99, SubscriptionAllocationTestData.Reason, SubscriptionAllocationTestData.ActorId, operationId, new string('f', 64)));
    }
}
