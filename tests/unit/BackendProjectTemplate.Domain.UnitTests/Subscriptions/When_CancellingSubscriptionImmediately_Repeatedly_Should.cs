namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CancellingSubscriptionImmediately_Repeatedly_Should
{
    [Fact]
    public void ReturnFalseWithoutRecordingAnotherTransition()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var subscription = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, SubscriptionAllocationTestData.Now.AddDays(3));
        var firstCancellation = SubscriptionAllocationTestData.Now.AddHours(1);

        subscription.CancelImmediately(firstCancellation, Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "First").ShouldBeTrue();
        subscription.CancelImmediately(firstCancellation.AddHours(1), Guid.CreateVersion7(), SubscriptionAllocationTestData.ActorId, "Second").ShouldBeFalse();

        subscription.StatusHistory.Count.ShouldBe(2);
        subscription.CancelledAtUtc.ShouldBe(firstCancellation);
        subscription.CancellationReason.ShouldBe("First");
    }
}
