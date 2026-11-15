using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_AllocatingSubscription_WithImmediateStart_Should
{
    [Fact]
    public void ActivateNonRenewingEntitlementWithoutPaymentIdentity()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan();
        var operationId = Guid.CreateVersion7();
        var start = SubscriptionAllocationTestData.Now;
        var end = start.AddDays(30);

        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, end, operationId);

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.AutoRenew.ShouldBeFalse();
        subscription.NextBillingAtUtc.ShouldBeNull();
        subscription.LastSuccessfulPaymentTransactionId.ShouldBeNull();
        subscription.CurrentPeriodStartUtc.ShouldBe(start);
        subscription.CurrentPeriodEndUtc.ShouldBe(end);
        subscription.EntitlementStartUtc.ShouldBe(start);
        subscription.EntitlementEndUtc.ShouldBe(end);
        var period = subscription.Periods.ShouldHaveSingleItem();
        period.PaymentTransactionId.ShouldBeNull();
        period.ActivationSource.ShouldBe(SubscriptionActivationSource.AdministrativeAllocation);
        period.OperationId.ShouldBe(operationId);
        period.Price.ShouldBe(plan.Price);
        var history = subscription.StatusHistory.ShouldHaveSingleItem();
        history.PreviousStatus.ShouldBeNull();
        history.NewStatus.ShouldBe(SubscriptionStatus.Active);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.AdministrativeAllocation);
        history.ActorId.ShouldBe(SubscriptionAllocationTestData.ActorId);
        history.Details.ShouldBe(SubscriptionAllocationTestData.Reason);
        history.PaymentTransactionId.ShouldBeNull();
    }
}
