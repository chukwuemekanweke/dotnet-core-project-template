using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ActivatingScheduledAllocation_Should
{
    [Fact]
    public void ActivateFromAgreedStartUsingTheAllocationSnapshot()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan(price: 700000m);
        var start = SubscriptionAllocationTestData.Now.AddDays(1);
        var end = start.AddDays(14);
        var operationId = Guid.CreateVersion7();
        var subscription = SubscriptionAllocationTestData.Allocate(plan, start, end, operationId);
        var allocation = SubscriptionAllocation.Create(
            subscription, plan, SubscriptionAllocationSource.Trial, SubscriptionAllocationTestData.Reason, SubscriptionAllocationTestData.ActorId, operationId, new string('f', 64));
        plan.SetActive(false);
        plan.UpdateTerms("Repriced", 900000m);
        var lateDelivery = start.AddHours(6);
        var activationOperationId = Guid.CreateVersion7();

        var period = subscription.ActivateScheduledAllocation(allocation, lateDelivery, activationOperationId);

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        period.PeriodStartUtc.ShouldBe(start);
        period.PeriodEndUtc.ShouldBe(end);
        period.Price.ShouldBe(700000m);
        period.PaymentTransactionId.ShouldBeNull();
        period.OperationId.ShouldBe(operationId);
        subscription.CurrentPeriodEndUtc.ShouldBe(end);
        subscription.NextBillingAtUtc.ShouldBeNull();
        var history = subscription.StatusHistory.Single(item => item.OperationId == activationOperationId);
        history.PreviousStatus.ShouldBe(SubscriptionStatus.Scheduled);
        history.NewStatus.ShouldBe(SubscriptionStatus.Active);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.ScheduledAllocationActivated);
    }
}
