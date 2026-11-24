using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_ExpiringUnpaidSubscriptionPurchase_Should
{
    private static readonly DateTimeOffset InitiatedAt = new(2026, 11, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MovePendingSubscriptionToExpiredWithAuditableHistory()
    {
        var subscription = CreatePending();
        var paymentId = Guid.CreateVersion7();
        var operationId = Guid.CreateVersion7();
        var expiredAt = InitiatedAt.AddMinutes(30);

        var expired = subscription.ExpireUnpaidPurchase(paymentId, expiredAt, operationId);

        expired.ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Expired);
        subscription.EndedAtUtc.ShouldBe(expiredAt);
        subscription.EntitlementStartUtc.ShouldBeNull();
        subscription.CurrentPeriodEndUtc.ShouldBeNull();
        var history = subscription.StatusHistory.Single(item => item.OperationId == operationId);
        history.PreviousStatus.ShouldBe(SubscriptionStatus.Pending);
        history.NewStatus.ShouldBe(SubscriptionStatus.Expired);
        history.Reason.ShouldBe(SubscriptionLifecycleReasons.InitialPaymentUnsuccessful);
        history.PaymentTransactionId.ShouldBe(paymentId);
    }

    [Fact]
    public void LeaveActiveSubscriptionUntouched()
    {
        var plan = SubscriptionPlan.Create("MONTHLY", "Monthly", 250000m, Guid.CreateVersion7(), BillingInterval.Monthly, true);
        var subscription = CreatePending(plan.Id);
        var paidPaymentId = Guid.CreateVersion7();
        var purchase = SubscriptionPurchase.Create(
            subscription.TenantId,
            subscription.BillingAccountId,
            subscription.Id,
            plan,
            plan.Prices.Single().BillingInterval,
            paidPaymentId,
            Guid.CreateVersion7());
        subscription.ActivateFromPayment(purchase, paidPaymentId, InitiatedAt, purchase.Id);
        var historyCount = subscription.StatusHistory.Count;

        var expired = subscription.ExpireUnpaidPurchase(Guid.CreateVersion7(), InitiatedAt.AddHours(1), Guid.CreateVersion7());

        expired.ShouldBeFalse();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.StatusHistory.Count.ShouldBe(historyCount);
    }

    [Fact]
    public void BeIdempotentOnceExpired()
    {
        var subscription = CreatePending();
        subscription.ExpireUnpaidPurchase(Guid.CreateVersion7(), InitiatedAt.AddMinutes(30), Guid.CreateVersion7());

        var expiredAgain = subscription.ExpireUnpaidPurchase(Guid.CreateVersion7(), InitiatedAt.AddHours(1), Guid.CreateVersion7());

        expiredAgain.ShouldBeFalse();
        subscription.EndedAtUtc.ShouldBe(InitiatedAt.AddMinutes(30));
        subscription.StatusHistory.Count.ShouldBe(2);
    }

    private static SubscriptionAggregate CreatePending(Guid? planId = null) =>
        SubscriptionAggregate.CreatePending(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            planId ?? Guid.CreateVersion7(),
            BillingInterval.Monthly,
            false,
            InitiatedAt,
            Guid.CreateVersion7(),
            Guid.CreateVersion7().ToString());
}
