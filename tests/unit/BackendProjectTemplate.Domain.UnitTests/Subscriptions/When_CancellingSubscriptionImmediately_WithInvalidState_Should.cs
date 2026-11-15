using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CancellingSubscriptionImmediately_WithInvalidState_Should
{
    [Fact]
    public void RejectPendingOrExpiredSubscriptionsAndMissingReason()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var pending = SubscriptionAggregate.CreatePending(
            Guid.CreateVersion7(), Guid.CreateVersion7(), plan.Id, true, SubscriptionAllocationTestData.Now, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId);
        var expired = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, SubscriptionAllocationTestData.Now.AddDays(1));
        expired.End(SubscriptionAllocationTestData.Now.AddDays(1), Guid.CreateVersion7());
        var active = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, SubscriptionAllocationTestData.Now.AddDays(1));

        Should.Throw<AggregateStateException>(() =>
            pending.CancelImmediately(SubscriptionAllocationTestData.Now, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "reason"));
        Should.Throw<AggregateStateException>(() =>
            expired.CancelImmediately(SubscriptionAllocationTestData.Now.AddDays(2), Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "reason"));
        Should.Throw<ArgumentException>(() =>
            active.CancelImmediately(SubscriptionAllocationTestData.Now, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, " "));
        expired.Status.ShouldBe(SubscriptionStatus.Expired);
        active.Status.ShouldBe(SubscriptionStatus.Active);
    }
}
