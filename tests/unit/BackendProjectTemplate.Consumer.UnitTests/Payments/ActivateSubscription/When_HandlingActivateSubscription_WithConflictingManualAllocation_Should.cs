using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithConflictingManualAllocation_Should
{
    [Fact]
    public async Task PreservePaymentAndBlockFulfillmentForReconciliation()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());
        context.SubscriptionRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>()).Returns(true);
        var purchase = context.SeededPurchase;
        var subscription = context.SeededSubscription;

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        purchase!.IsFulfillmentBlocked.ShouldBeTrue();
        purchase.FulfillmentBlockedReason.ShouldBe(SubscriptionFulfillmentBlockReasons.EntitlementConflict);
        purchase.FulfilledAtUtc.ShouldBeNull();
        subscription!.Status.ShouldBe(SubscriptionStatus.Pending);
        await context.SubscriptionActivationRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await context.SubscriptionLifecyclePersistence.DidNotReceiveWithAnyArgs().AddPeriodAsync(default!, default);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
