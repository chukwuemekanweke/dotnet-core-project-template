using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_SchedulingSubscriptionCancellation_Should
{
    [Fact]
    public void PreserveEntitlementAndBeIdempotent()
    {
        var tenantId = Guid.CreateVersion7();
        var billingAccountId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var now = DateTimeOffset.Parse("2026-11-14T12:00:00Z");
        var plan = SubscriptionPlan.Create("MONTHLY", "Monthly", 500000m, Guid.CreateVersion7(), BillingInterval.Monthly, true);
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
        subscription.ActivateFromPayment(purchase, paymentId, now, purchase.Id);
        var entitledUntil = subscription.CurrentPeriodEndUtc;

        subscription.ScheduleCancellation(now.AddDays(1), Guid.CreateVersion7(), stakeholderId.ToString()).ShouldBeTrue();
        subscription.ScheduleCancellation(now.AddDays(2), Guid.CreateVersion7(), stakeholderId.ToString()).ShouldBeFalse();

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CurrentPeriodEndUtc.ShouldBe(entitledUntil);
        subscription.CancelAtPeriodEnd.ShouldBeTrue();
        subscription.AutoRenew.ShouldBeFalse();
        subscription.NextBillingAtUtc.ShouldBeNull();
        subscription.StatusHistory.Count.ShouldBe(3);
    }
}
