using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_AllocatingSubscription_WithFutureStart_Should
{
    [Fact]
    public void ReserveTheWindowWithoutGrantingEntitlement()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var start = SubscriptionAllocationTestData.Now.AddDays(10);
        var end = start.AddDays(30);

        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, end);

        subscription.Status.ShouldBe(SubscriptionStatus.Scheduled);
        subscription.Periods.ShouldBeEmpty();
        subscription.CurrentPeriodStartUtc.ShouldBeNull();
        subscription.CurrentPeriodEndUtc.ShouldBeNull();
        subscription.EntitlementStartUtc.ShouldBe(start);
        subscription.EntitlementEndUtc.ShouldBe(end);
        subscription.AutoRenew.ShouldBeFalse();
        var history = subscription.StatusHistory.ShouldHaveSingleItem();
        history.NewStatus.ShouldBe(SubscriptionStatus.Scheduled);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.AllocationScheduled);
    }
}
