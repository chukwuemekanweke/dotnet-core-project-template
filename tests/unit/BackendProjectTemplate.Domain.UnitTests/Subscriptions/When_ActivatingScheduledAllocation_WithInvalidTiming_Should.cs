using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ActivatingScheduledAllocation_WithInvalidTiming_Should
{
    [Fact]
    public void RejectEarlyOrRepeatedActivation()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var start = SubscriptionAllocationTestData.Now.AddDays(1);
        var operationId = Guid.CreateVersion7();
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, start.AddDays(5), operationId);
        var allocation = SubscriptionAllocation.Create(
            subscription, plan, SubscriptionAllocationSource.AdminAllocation, SubscriptionAllocationTestData.Reason, SubscriptionAllocationTestData.ActorId, operationId, new string('f', 64));

        Should.Throw<AggregateStateException>(() =>
            subscription.ActivateScheduledAllocation(allocation, start.AddTicks(-1), Guid.CreateVersion7()));
        subscription.ActivateScheduledAllocation(allocation, start, Guid.CreateVersion7());
        Should.Throw<AggregateStateException>(() =>
            subscription.ActivateScheduledAllocation(allocation, start.AddMinutes(1), Guid.CreateVersion7()));
        subscription.Periods.Count.ShouldBe(1);
    }
}
