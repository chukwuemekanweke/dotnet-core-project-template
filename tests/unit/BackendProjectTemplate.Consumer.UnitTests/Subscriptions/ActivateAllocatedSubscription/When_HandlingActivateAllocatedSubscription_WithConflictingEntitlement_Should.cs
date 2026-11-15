using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithConflictingEntitlement_Should
{
    [Fact]
    public async Task LeaveTheAllocationScheduled()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var context = new SubscriptionsConsumerTestContext(start);
        var (subscription, allocation) = context.CreateScheduledAllocation(start, start.AddDays(30));
        context.SubscriptionRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>()).Returns(true);

        await context.CreateActivationHandler().HandleAsync(context.CreateActivation(subscription, allocation), CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Scheduled);
        await context.LifecyclePersistence.DidNotReceiveWithAnyArgs().AddPeriodAsync(default!, default);
        await context.CommandSender.DidNotReceiveWithAnyArgs().ScheduleSendAsync<ExpireSubscriptionCommand>(default!, default, default);
    }
}
