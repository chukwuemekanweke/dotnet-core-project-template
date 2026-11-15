using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_BlockingSubscriptionPurchaseFulfillment_Should
{
    [Fact]
    public void FlagThePurchaseUntilItIsFulfilled()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var purchase = SubscriptionPurchase.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), plan, Guid.CreateVersion7(), Guid.CreateVersion7());

        purchase.MarkFulfillmentBlocked(SubscriptionFulfillmentBlockReasons.EntitlementConflict, SubscriptionAllocationTestData.Now);

        purchase.IsFulfillmentBlocked.ShouldBeTrue();
        purchase.FulfillmentBlockedReason.ShouldBe(SubscriptionFulfillmentBlockReasons.EntitlementConflict);
        purchase.MarkFulfilled(SubscriptionAllocationTestData.Now.AddDays(1)).ShouldBeTrue();
        purchase.IsFulfillmentBlocked.ShouldBeFalse();
        purchase.FulfillmentBlockedAtUtc.ShouldBe(SubscriptionAllocationTestData.Now);
        Should.Throw<AggregateStateException>(() =>
            purchase.MarkFulfillmentBlocked(SubscriptionFulfillmentBlockReasons.EntitlementConflict, SubscriptionAllocationTestData.Now));
    }
}
