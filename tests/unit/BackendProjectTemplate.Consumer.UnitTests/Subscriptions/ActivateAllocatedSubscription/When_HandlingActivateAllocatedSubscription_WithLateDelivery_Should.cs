using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithLateDelivery_Should
{
    [Fact]
    public async Task ActivateFromTheAgreedStartWithoutExtendingTheGrant()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var end = start.AddDays(7);
        var context = new SubscriptionsConsumerTestContext(start.AddDays(2));
        var (subscription, allocation) = context.CreateScheduledAllocation(start, end);

        await context.CreateActivationHandler().HandleAsync(context.CreateActivation(subscription, allocation), CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CurrentPeriodStartUtc.ShouldBe(start);
        subscription.CurrentPeriodEndUtc.ShouldBe(end);
    }
}
