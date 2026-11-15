using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CancellingSubscriptionImmediately_BeforeScheduledActivation_Should
{
    [Fact]
    public void CancelTheReservationAndRejectLaterActivation()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var start = SubscriptionAllocationTestData.Now.AddDays(7);
        var operationId = Guid.CreateVersion7();
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, start.AddDays(30), operationId);
        var allocation = SubscriptionAllocation.Create(
            subscription, plan, SubscriptionAllocationSource.Migration, SubscriptionAllocationTestData.Reason, SubscriptionAllocationTestData.ActorId, operationId, new string('f', 64));

        subscription.CancelImmediately(SubscriptionAllocationTestData.Now, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "Customer migrated elsewhere").ShouldBeTrue();

        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
        subscription.Periods.ShouldBeEmpty();
        subscription.EndedAtUtc.ShouldBe(SubscriptionAllocationTestData.Now);
        Should.Throw<AggregateStateException>(() =>
            subscription.ActivateScheduledAllocation(allocation, start, Guid.CreateVersion7()));
    }
}
