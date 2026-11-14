using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionPeriod : Entity
{
    private SubscriptionPeriod()
    {
    }

    internal SubscriptionPeriod(
        Guid subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        Guid planId,
        string planCode,
        decimal price,
        Guid currencyId,
        BillingInterval billingInterval,
        Guid? paymentTransactionId,
        SubscriptionActivationSource activationSource,
        Guid operationId)
    {
        if (periodEndUtc <= periodStartUtc)
        {
            throw new ArgumentException("Subscription period end must be after its start.", nameof(periodEndUtc));
        }

        SubscriptionId = subscriptionId;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        PlanId = planId;
        PlanCode = planCode;
        Price = price;
        CurrencyId = currencyId;
        BillingInterval = billingInterval;
        PaymentTransactionId = paymentTransactionId;
        ActivationSource = activationSource;
        OperationId = operationId;
    }

    public Guid SubscriptionId { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }
    public Guid PlanId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CurrencyId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public SubscriptionActivationSource ActivationSource { get; private set; }
    public Guid OperationId { get; private set; }
}
