using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ActivatingSubscription_FromPayment_Should
{
    [Fact]
    public void CreatePeriodAndDurableHistoryFromPurchaseSnapshot()
    {
        var tenantId = Guid.CreateVersion7();
        var billingAccountId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var initiatedAt = new DateTimeOffset(2026, 1, 31, 8, 0, 0, TimeSpan.Zero);
        var plan = SubscriptionPlan.Create("MONTHLY", "Monthly", 250000m, currencyId, BillingInterval.Monthly, true);
        var subscription = SubscriptionAggregate.CreatePending(
            tenantId,
            billingAccountId,
            plan.Id,
            true,
            initiatedAt,
            Guid.CreateVersion7(),
            stakeholderId.ToString());
        var purchase = SubscriptionPurchase.Create(
            tenantId,
            billingAccountId,
            subscription.Id,
            plan,
            paymentId,
            stakeholderId);

        plan.ChangePrice(300000m);
        var period = subscription.ActivateFromPayment(purchase, paymentId, initiatedAt, purchase.Id);

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CurrentPeriodEndUtc.ShouldBe(new DateTimeOffset(2026, 2, 28, 8, 0, 0, TimeSpan.Zero));
        subscription.NextBillingAtUtc.ShouldBe(subscription.CurrentPeriodEndUtc);
        subscription.LastSuccessfulPaymentTransactionId.ShouldBe(paymentId);
        period.Price.ShouldBe(250000m);
        period.CurrencyId.ShouldBe(currencyId);
        subscription.StatusHistory.Count.ShouldBe(2);
    }
}
