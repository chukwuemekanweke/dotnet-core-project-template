using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_SchedulingAdministrativeCancellation_Should
{
    [Fact]
    public void PreserveEntitlementUntilPeriodEndAndRecordJustification()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var end = SubscriptionAllocationTestData.Now.AddDays(20);
        var subscription = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, end);
        var operationId = Guid.CreateVersion7();

        subscription.ScheduleCancellation(SubscriptionAllocationTestData.Now.AddDays(1), operationId, SubscriptionAllocationTestData.ActorId, "Contract not renewed").ShouldBeTrue();
        subscription.ScheduleCancellation(SubscriptionAllocationTestData.Now.AddDays(2), Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "Again").ShouldBeFalse();

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CancelAtPeriodEnd.ShouldBeTrue();
        subscription.EntitlementEndUtc.ShouldBe(end);
        subscription.CancellationReason.ShouldBe("Contract not renewed");
        var history = subscription.StatusHistory.Single(item => item.OperationId == operationId);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.AdministrativeCancellationScheduled);
        history.Details.ShouldBe("Contract not renewed");

        subscription.End(end, Guid.CreateVersion7());
        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
        subscription.Periods.ShouldHaveSingleItem();
    }
}
