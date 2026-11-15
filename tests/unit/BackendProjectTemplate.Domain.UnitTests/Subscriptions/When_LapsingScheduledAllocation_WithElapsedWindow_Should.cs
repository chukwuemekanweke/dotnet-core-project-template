using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_LapsingScheduledAllocation_WithElapsedWindow_Should
{
    [Fact]
    public void ExpireWithoutEverGrantingEntitlement()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var start = SubscriptionAllocationTestData.Now.AddDays(1);
        var end = start.AddDays(2);
        var operationId = Guid.CreateVersion7();
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, end, operationId);
        var allocation = SubscriptionAllocation.Create(
            subscription, plan, SubscriptionAllocationSource.Trial, SubscriptionAllocationTestData.Reason, SubscriptionAllocationTestData.ActorId, operationId, new string('f', 64));
        var lapseOperationId = Guid.CreateVersion7();

        Should.Throw<AggregateStateException>(() => subscription.ActivateScheduledAllocation(allocation, end, Guid.CreateVersion7()));
        Should.Throw<AggregateStateException>(() => subscription.LapseScheduledAllocation(allocation, end.AddTicks(-10), Guid.CreateVersion7()));
        subscription.LapseScheduledAllocation(allocation, end.AddHours(3), lapseOperationId);

        subscription.Status.ShouldBe(SubscriptionStatus.Expired);
        subscription.Periods.ShouldBeEmpty();
        subscription.CurrentPeriodStartUtc.ShouldBeNull();
        subscription.EndedAtUtc.ShouldBe(end.AddHours(3));
        var history = subscription.StatusHistory.Single(item => item.OperationId == lapseOperationId);
        history.PreviousStatus.ShouldBe(SubscriptionStatus.Scheduled);
        history.NewStatus.ShouldBe(SubscriptionStatus.Expired);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.ScheduledAllocationLapsed);
        subscription.StatusHistory.ShouldNotContain(item => item.NewStatus == SubscriptionStatus.Active);
        Should.Throw<AggregateStateException>(() => subscription.LapseScheduledAllocation(allocation, end.AddDays(1), Guid.CreateVersion7()));
    }
}
