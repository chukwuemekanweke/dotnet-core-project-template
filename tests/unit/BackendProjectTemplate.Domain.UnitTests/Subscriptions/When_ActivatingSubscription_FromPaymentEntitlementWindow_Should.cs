using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ActivatingSubscription_FromPaymentEntitlementWindow_Should
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HoldOpenEndedEntitlementOnlyWhileRenewing(bool autoRenew)
    {
        var tenantId = Guid.CreateVersion7();
        var billingAccountId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var subscription = SubscriptionAggregate.CreatePending(
            tenantId, billingAccountId, plan.Id, autoRenew, SubscriptionAllocationTestData.Now, Guid.CreateVersion7(), stakeholderId.ToString());
        var purchase = SubscriptionPurchase.Create(tenantId, billingAccountId, subscription.Id, plan, paymentId, stakeholderId);
        var projected = subscription.ProjectPaidEntitlement(purchase, SubscriptionAllocationTestData.Now);

        subscription.ActivateFromPayment(purchase, paymentId, SubscriptionAllocationTestData.Now, purchase.Id);

        subscription.EntitlementStartUtc.ShouldBe(projected.StartUtc);
        subscription.EntitlementEndUtc.ShouldBe(projected.EndUtc);
        subscription.EntitlementEndUtc.ShouldBe(autoRenew ? null : subscription.CurrentPeriodEndUtc);
        if (autoRenew)
        {
            subscription.ScheduleCancellation(SubscriptionAllocationTestData.Now.AddDays(1), Guid.CreateVersion7(), stakeholderId.ToString());
            subscription.EntitlementEndUtc.ShouldBe(subscription.CurrentPeriodEndUtc);
        }
    }
}
