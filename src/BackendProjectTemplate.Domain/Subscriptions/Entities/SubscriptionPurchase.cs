using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionPurchase : Entity, IAggregateRoot
{
    public const int MaxPlanCodeLength = SubscriptionPlan.MaxCodeLength;
    public const int MaxRequestFingerprintLength = 64;

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
        Guid initiatedByStakeholderId,
        Guid operationId,
        string requestFingerprint)
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
        OperationId = operationId;
        RequestFingerprint = requestFingerprint;
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
    public Guid OperationId { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset? FulfilledAtUtc { get; private set; }

    /// <summary>
    /// Set when a verified payment could not be fulfilled (for example because the BillingAccount already
    /// holds a conflicting entitlement). The payment record is preserved and the purchase stays unfulfilled
    /// until an administrator resolves the conflict and retries fulfillment, or refunds outside this domain.
    /// </summary>
    public DateTimeOffset? FulfillmentBlockedAtUtc { get; private set; }
    public string? FulfillmentBlockedReason { get; private set; }

    public bool IsFulfillmentBlocked => FulfillmentBlockedAtUtc.HasValue && !FulfilledAtUtc.HasValue;

    public static SubscriptionPurchase Create(
        Guid tenantId,
        Guid billingAccountId,
        Guid subscriptionId,
        SubscriptionPlan plan,
        Guid paymentTransactionId,
        Guid initiatedByStakeholderId,
        Guid? operationId = null,
        string? requestFingerprint = null) =>
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
            initiatedByStakeholderId,
            operationId ?? Guid.CreateVersion7(),
            NormalizeFingerprint(requestFingerprint, operationId));

    private static string NormalizeFingerprint(string? requestFingerprint, Guid? operationId)
    {
        var normalized = string.IsNullOrWhiteSpace(requestFingerprint)
            ? (operationId ?? Guid.Empty).ToString("N")
            : requestFingerprint.Trim();
        return normalized.Length <= MaxRequestFingerprintLength
            ? normalized
            : throw new ArgumentException(
                $"Request fingerprint must not exceed {MaxRequestFingerprintLength} characters.",
                nameof(requestFingerprint));
    }

    public bool MarkFulfilled(DateTimeOffset fulfilledAtUtc)
    {
        if (FulfilledAtUtc.HasValue)
        {
            return false;
        }

        FulfilledAtUtc = fulfilledAtUtc;
        return true;
    }

    public void MarkFulfillmentBlocked(string reason, DateTimeOffset blockedAtUtc)
    {
        if (FulfilledAtUtc.HasValue)
        {
            throw new AggregateStateException($"Purchase '{Id}' is already fulfilled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A fulfillment block reason is required.", nameof(reason));
        }

        FulfillmentBlockedAtUtc = blockedAtUtc;
        FulfillmentBlockedReason = reason.Trim();
    }

    public void ValidatePayment(Guid tenantId, decimal amount, Guid currencyId)
    {
        if (TenantId != tenantId || ExpectedAmount != amount || ExpectedCurrencyId != currencyId)
        {
            throw new AggregateStateException($"Payment does not match subscription purchase '{Id}'.");
        }
    }
}
