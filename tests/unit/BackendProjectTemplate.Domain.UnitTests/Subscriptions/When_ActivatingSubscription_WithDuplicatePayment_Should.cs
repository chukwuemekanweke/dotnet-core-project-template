using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ActivatingSubscription_WithDuplicatePayment_Should
{
    [Fact]
    public void ReturnExistingPeriodWithoutDuplicateHistory()
    {
        var tenantId = Guid.CreateVersion7();
        var billingAccountId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var now = DateTimeOffset.Parse("2026-11-14T12:00:00Z");
        var plan = SubscriptionPlan.Create("YEARLY", "Yearly", 1000000m, Guid.CreateVersion7(), BillingInterval.Yearly, true);
        var subscription = SubscriptionAggregate.CreatePending(
            tenantId,
            billingAccountId,
            plan.Id,
            true,
            now,
            Guid.CreateVersion7(),
            stakeholderId.ToString());
        var purchase = SubscriptionPurchase.Create(
            tenantId,
            billingAccountId,
            subscription.Id,
            plan,
            paymentId,
            stakeholderId);

        var first = subscription.ActivateFromPayment(purchase, paymentId, now, purchase.Id);
        var duplicate = subscription.ActivateFromPayment(purchase, paymentId, now.AddMinutes(1), purchase.Id);

        duplicate.ShouldBeSameAs(first);
        subscription.Periods.Count.ShouldBe(1);
        subscription.StatusHistory.Count.ShouldBe(2);
    }
}
