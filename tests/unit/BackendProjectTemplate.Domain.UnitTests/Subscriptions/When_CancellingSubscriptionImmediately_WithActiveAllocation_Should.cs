using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CancellingSubscriptionImmediately_WithActiveAllocation_Should
{
    [Fact]
    public void TerminateEntitlementNowAndPreserveGrantedPeriod()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var start = SubscriptionAllocationTestData.Now;
        var end = start.AddDays(30);
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, end);
        var cancelledAt = start.AddDays(4);
        var operationId = Guid.CreateVersion7();

        subscription.CancelImmediately(cancelledAt, operationId, SubscriptionAllocationTestData.ActorId, "Abuse of complimentary access").ShouldBeTrue();

        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
        subscription.CancelledAtUtc.ShouldBe(cancelledAt);
        subscription.EndedAtUtc.ShouldBe(cancelledAt);
        subscription.EntitlementEndUtc.ShouldBe(cancelledAt);
        subscription.CurrentPeriodEndUtc.ShouldBe(end);
        subscription.AutoRenew.ShouldBeFalse();
        subscription.NextBillingAtUtc.ShouldBeNull();
        subscription.CancellationReason.ShouldBe("Abuse of complimentary access");
        subscription.Periods.ShouldHaveSingleItem().PeriodEndUtc.ShouldBe(end);
        var history = subscription.StatusHistory.Single(item => item.OperationId == operationId);
        history.PreviousStatus.ShouldBe(SubscriptionStatus.Active);
        history.NewStatus.ShouldBe(SubscriptionStatus.Cancelled);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.AdministrativeImmediateCancellation);
        history.ActorId.ShouldBe(SubscriptionAllocationTestData.ActorId);
        history.Details.ShouldBe("Abuse of complimentary access");
        Should.Throw<AggregateStateException>(() => subscription.End(end, Guid.CreateVersion7()));
    }
}
