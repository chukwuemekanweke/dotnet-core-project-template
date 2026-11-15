using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithElapsedWindow_Should
{
    [Fact]
    public async Task LapseToExpiredWithoutPassingThroughActive()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var end = start.AddDays(7);
        var context = new SubscriptionsConsumerTestContext(end.AddMinutes(1));
        var (subscription, allocation) = context.CreateScheduledAllocation(start, end);
        var command = context.CreateActivation(subscription, allocation);

        await context.CreateActivationHandler().HandleAsync(command, CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Expired);
        subscription.Periods.ShouldBeEmpty();
        await context.LifecyclePersistence.DidNotReceiveWithAnyArgs().AddPeriodAsync(default!, default);
        await context.LifecyclePersistence.Received(1).AddStatusHistoryAsync(
            Arg.Is<SubscriptionStatusHistory>(history =>
                history.OperationId == command.MessageId &&
                history.PreviousStatus == SubscriptionStatus.Scheduled &&
                history.NewStatus == SubscriptionStatus.Expired &&
                history.Reason == SubscriptionLifecycleReasons.ScheduledAllocationLapsed),
            Arg.Any<CancellationToken>());
        await context.CommandSender.DidNotReceiveWithAnyArgs().ScheduleSendAsync<ExpireSubscriptionCommand>(default!, default, default);
    }
}
