using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class SubscriptionActivation : Entity, IAggregateRoot
{
    private SubscriptionActivation()
    {
    }

    private SubscriptionActivation(
        Guid paymentTransactionId,
        Guid billingAccountId,
        Guid stakeholderId,
        Guid tenantId,
        string? subscriptionReference,
        decimal amount,
        Guid currencyId,
        DateTimeOffset activatedAtUtc)
    {
        PaymentTransactionId = paymentTransactionId;
        BillingAccountId = billingAccountId;
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        SubscriptionReference = string.IsNullOrWhiteSpace(subscriptionReference) ? null : subscriptionReference.Trim();
        Amount = amount;
        CurrencyId = currencyId;
        ActivatedAtUtc = activatedAtUtc;
    }

    public Guid PaymentTransactionId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public string? SubscriptionReference { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateTimeOffset ActivatedAtUtc { get; private set; }

    public static SubscriptionActivation Create(
        Guid paymentTransactionId,
        Guid billingAccountId,
        Guid stakeholderId,
        Guid tenantId,
        string? subscriptionReference,
        decimal amount,
        Guid currencyId,
        DateTimeOffset activatedAtUtc) =>
        new(paymentTransactionId, billingAccountId, stakeholderId, tenantId, subscriptionReference, amount, currencyId, activatedAtUtc);
}
