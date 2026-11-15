using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithStaleOrDuplicateCommand_Should
{
    [Fact]
    public async Task IgnoreCancelledAlreadyActiveOrMismatchedSubscriptions()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var context = new SubscriptionsConsumerTestContext(start);
        var handler = context.CreateActivationHandler();

        var (cancelled, cancelledAllocation) = context.CreateScheduledAllocation(start, start.AddDays(30));
        cancelled.CancelImmediately(start.AddDays(-1), Guid.CreateVersion7(), context.ActorId, "Withdrawn");
        await handler.HandleAsync(context.CreateActivation(cancelled, cancelledAllocation), CancellationToken.None);
        cancelled.Status.ShouldBe(SubscriptionStatus.Cancelled);

        var (active, activeAllocation) = context.CreateScheduledAllocation(start, start.AddDays(30));
        await handler.HandleAsync(context.CreateActivation(active, activeAllocation), CancellationToken.None);
        await handler.HandleAsync(context.CreateActivation(active, activeAllocation), CancellationToken.None);
        active.Periods.Count.ShouldBe(1);

        var (other, otherAllocation) = context.CreateScheduledAllocation(start, start.AddDays(30));
        await handler.HandleAsync(
            context.CreateActivation(other, otherAllocation) with { TenantId = Guid.CreateVersion7() },
            CancellationToken.None);
        await handler.HandleAsync(
            context.CreateActivation(other, otherAllocation) with { ExpectedStartUtc = start.AddDays(-1) },
            CancellationToken.None);
        other.Status.ShouldBe(SubscriptionStatus.Scheduled);

        await context.LifecyclePersistence.Received(1).AddPeriodAsync(Arg.Any<SubscriptionPeriod>(), Arg.Any<CancellationToken>());
    }
}
