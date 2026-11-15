using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ExpireSubscription;

public sealed class When_HandlingExpireSubscription_WithElapsedAllocation_Should
{
    [Fact]
    public async Task ExpireOnlyWhenTheGrantedPeriodHasEnded()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt;
        var end = start.AddDays(10);
        var context = new SubscriptionsConsumerTestContext(end.AddMinutes(-1));
        var subscription = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            context.TenantId, context.BillingAccountId, context.Plan, start, end, start, Guid.CreateVersion7(), context.ActorId, "Grant");
        context.SubscriptionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Subscriptions.Entities.Subscription>>(), Arg.Any<CancellationToken>())
            .Returns(subscription);
        var handler = context.CreateExpireHandler();

        await handler.HandleAsync(new ExpireSubscriptionCommand(subscription.Id, end) { TenantId = context.TenantId }, CancellationToken.None);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);

        context.Clock.UtcNow = end;
        await handler.HandleAsync(new ExpireSubscriptionCommand(subscription.Id, end) { TenantId = context.TenantId }, CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Expired);
        subscription.EndedAtUtc.ShouldBe(end);
        await context.LifecyclePersistence.Received(1).AddStatusHistoryAsync(
            Arg.Is<SubscriptionStatusHistory>(history => history.NewStatus == SubscriptionStatus.Expired),
            Arg.Any<CancellationToken>());
    }
}
