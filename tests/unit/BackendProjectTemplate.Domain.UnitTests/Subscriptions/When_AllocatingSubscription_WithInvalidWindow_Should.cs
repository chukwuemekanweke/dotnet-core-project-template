using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_AllocatingSubscription_WithInvalidWindow_Should
{
    [Fact]
    public void RejectNonPositiveOrElapsedWindowsAndMissingAuditData()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var now = SubscriptionAllocationTestData.Now;

        Should.Throw<ArgumentException>(() => SubscriptionAllocationTestData.Allocate(plan, now, now));
        Should.Throw<ArgumentException>(() => SubscriptionAllocationTestData.Allocate(plan, now.AddDays(2), now.AddDays(1)));
        Should.Throw<ArgumentException>(() => SubscriptionAllocationTestData.Allocate(plan, now.AddDays(-10), now.AddDays(-1)));
        Should.Throw<ArgumentException>(() => SubscriptionAggregate.CreateAllocated(
            Guid.CreateVersion7(), Guid.CreateVersion7(), plan, now, now.AddDays(1), now, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "  "));
        Should.Throw<ArgumentException>(() => SubscriptionAggregate.CreateAllocated(
            Guid.CreateVersion7(), Guid.CreateVersion7(), plan, now, now.AddDays(1), now, Guid.CreateVersion7(), " ", SubscriptionAllocationTestData.Reason));
        Should.Throw<ArgumentException>(() => SubscriptionAggregate.CreateAllocated(
            Guid.CreateVersion7(), Guid.CreateVersion7(), plan, now, now.AddDays(1), now, Guid.Empty, SubscriptionAllocationTestData.ActorId, SubscriptionAllocationTestData.Reason));
    }
}
