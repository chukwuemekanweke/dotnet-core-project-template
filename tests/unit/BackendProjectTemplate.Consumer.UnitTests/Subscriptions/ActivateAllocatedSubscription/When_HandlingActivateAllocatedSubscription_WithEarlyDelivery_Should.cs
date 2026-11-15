using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithEarlyDelivery_Should
{
    [Fact]
    public async Task RescheduleForTheEntitlementStart()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var context = new SubscriptionsConsumerTestContext(start.AddMinutes(-5));
        var (subscription, allocation) = context.CreateScheduledAllocation(start, start.AddDays(30));

        await context.CreateActivationHandler().HandleAsync(context.CreateActivation(subscription, allocation), CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Scheduled);
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<ActivateAllocatedSubscriptionCommand>(activation => activation.SubscriptionId == subscription.Id && activation.TenantId == context.TenantId),
            start,
            Arg.Any<CancellationToken>());
        await context.LifecyclePersistence.DidNotReceiveWithAnyArgs().AddPeriodAsync(default!, default);
    }
}
