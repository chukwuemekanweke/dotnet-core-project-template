using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CreatingSubscriptionRenewal_Should
{
    [Fact]
    public void PreserveAuthoritativePeriodAndPriceSnapshot()
    {
        var now = new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);
        var currencyId = Guid.CreateVersion7();
        var plan = SubscriptionPlan.Create("MONTHLY", "Monthly", 5000m, currencyId, BillingInterval.Monthly, true);
        var subscription = Subscription.CreatePending(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            plan.Id,
            true,
            now,
            Guid.CreateVersion7(),
            null);
        var purchase = SubscriptionPurchase.Create(
            subscription.TenantId,
            subscription.BillingAccountId,
            subscription.Id,
            plan,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var period = subscription.ActivateFromPayment(purchase, purchase.PaymentTransactionId, now, purchase.Id);

        var renewal = SubscriptionRenewal.Create(subscription, period, now);

        renewal.SubscriptionId.ShouldBe(subscription.Id);
        renewal.TargetPeriodStartUtc.ShouldBe(new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero));
        renewal.TargetPeriodEndUtc.ShouldBe(new DateTimeOffset(2026, 3, 28, 12, 0, 0, TimeSpan.Zero));
        renewal.ExpectedPrice.ShouldBe(5000m);
        renewal.CurrencyId.ShouldBe(currencyId);
        renewal.Status.ShouldBe(SubscriptionRenewalStatus.Pending);
    }
}
