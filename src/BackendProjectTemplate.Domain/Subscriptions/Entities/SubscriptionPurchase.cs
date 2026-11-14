using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionPurchase : Entity, IAggregateRoot
{
    public const int MaxPlanCodeLength = SubscriptionPlan.MaxCodeLength;

    private SubscriptionPurchase()
    {
    }

    private SubscriptionPurchase(
        Guid tenantId,
        Guid billingAccountId,
        Guid subscriptionId,
        Guid planId,
        string planCode,
        decimal expectedAmount,
        Guid expectedCurrencyId,
        BillingInterval billingInterval,
        Guid paymentTransactionId,
        Guid initiatedByStakeholderId)
    {
        TenantId = tenantId;
        BillingAccountId = billingAccountId;
        SubscriptionId = subscriptionId;
        PlanId = planId;
        PlanCode = planCode;
        ExpectedAmount = expectedAmount;
        ExpectedCurrencyId = expectedCurrencyId;
        BillingInterval = billingInterval;
        PaymentTransactionId = paymentTransactionId;
        InitiatedByStakeholderId = initiatedByStakeholderId;
    }

    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid PlanId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public decimal ExpectedAmount { get; private set; }
    public Guid ExpectedCurrencyId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public Guid InitiatedByStakeholderId { get; private set; }
    public DateTimeOffset? FulfilledAtUtc { get; private set; }

    public static SubscriptionPurchase Create(
        Guid tenantId,
        Guid billingAccountId,
        Guid subscriptionId,
        SubscriptionPlan plan,
        Guid paymentTransactionId,
        Guid initiatedByStakeholderId) =>
        new(
            tenantId,
            billingAccountId,
            subscriptionId,
            plan.Id,
            plan.Code,
            plan.Price,
            plan.CurrencyId,
            plan.BillingInterval,
            paymentTransactionId,
            initiatedByStakeholderId);

    public bool MarkFulfilled(DateTimeOffset fulfilledAtUtc)
    {
        if (FulfilledAtUtc.HasValue)
        {
            return false;
        }

        FulfilledAtUtc = fulfilledAtUtc;
        return true;
    }

    public void ValidatePayment(Guid tenantId, decimal amount, Guid currencyId)
    {
        if (TenantId != tenantId || ExpectedAmount != amount || ExpectedCurrencyId != currencyId)
        {
            throw new AggregateStateException($"Payment does not match subscription purchase '{Id}'.");
        }
    }
}
