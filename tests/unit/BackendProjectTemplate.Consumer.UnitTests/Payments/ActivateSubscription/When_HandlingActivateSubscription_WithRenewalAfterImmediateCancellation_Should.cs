using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithRenewalAfterImmediateCancellation_Should
{
    [Fact]
    public async Task RecordTheChargeWithoutReactivatingTheSubscription()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());
        var purchase = context.SeededPurchase!;
        var subscription = context.SeededSubscription!;
        var initialPaymentId = Guid.CreateVersion7();
        subscription.ActivateFromPayment(purchase, initialPaymentId, context.Clock.GetUtcNow().AddMonths(-1), purchase.Id);
        var renewal = SubscriptionRenewal.Create(subscription, subscription.Periods.Single(), context.Clock.GetUtcNow());
        var attempt = renewal.StartAttempt(Guid.CreateVersion7(), command.PaymentTransactionId, "renewal-key", context.Clock.GetUtcNow());
        subscription.CancelImmediately(context.Clock.GetUtcNow(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString(), "Chargeback");
        context.SubscriptionPurchaseRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPurchase>>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionPurchase?)null);
        context.SubscriptionRenewalRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionRenewal>>(), Arg.Any<CancellationToken>())
            .Returns(renewal);

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
        subscription.Periods.Count.ShouldBe(1);
        renewal.Status.ShouldBe(SubscriptionRenewalStatus.FulfillmentBlocked);
        attempt.Status.ShouldBe(SubscriptionRenewalAttemptStatus.Succeeded);
        await context.SubscriptionLifecyclePersistence.DidNotReceiveWithAnyArgs().AddPeriodAsync(default!, default);
    }
}
