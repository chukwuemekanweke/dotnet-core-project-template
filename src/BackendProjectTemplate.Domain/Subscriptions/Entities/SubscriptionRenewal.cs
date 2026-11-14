using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionRenewal : Entity, IAggregateRoot
{
    private readonly List<SubscriptionRenewalAttempt> _attempts = [];

    private SubscriptionRenewal()
    {
    }

    private SubscriptionRenewal(
        Guid subscriptionId,
        Guid tenantId,
        Guid billingAccountId,
        DateTimeOffset targetPeriodStartUtc,
        DateTimeOffset targetPeriodEndUtc,
        decimal expectedPrice,
        Guid currencyId,
        BillingInterval billingInterval,
        DateTimeOffset createdAtUtc)
    {
        if (targetPeriodEndUtc <= targetPeriodStartUtc)
        {
            throw new ArgumentException("Renewal period end must be after its start.", nameof(targetPeriodEndUtc));
        }

        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        BillingAccountId = billingAccountId;
        TargetPeriodStartUtc = targetPeriodStartUtc;
        TargetPeriodEndUtc = targetPeriodEndUtc;
        ExpectedPrice = expectedPrice;
        CurrencyId = currencyId;
        BillingInterval = billingInterval;
        Status = SubscriptionRenewalStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid SubscriptionId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public DateTimeOffset TargetPeriodStartUtc { get; private set; }
    public DateTimeOffset TargetPeriodEndUtc { get; private set; }
    public decimal ExpectedPrice { get; private set; }
    public Guid CurrencyId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public SubscriptionRenewalStatus Status { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
    public int ReminderSequenceSent { get; private set; }
    public uint RowVersion { get; private set; }
    public IReadOnlyCollection<SubscriptionRenewalAttempt> Attempts => _attempts;

    public static SubscriptionRenewal Create(Subscription subscription, SubscriptionPeriod currentPeriod, DateTimeOffset createdAtUtc)
    {
        if (subscription.Id != currentPeriod.SubscriptionId || subscription.CurrentPeriodEndUtc != currentPeriod.PeriodEndUtc)
        {
            throw new AggregateStateException("The current subscription period does not match the renewal boundary.");
        }

        var targetEndUtc = currentPeriod.BillingInterval switch
        {
            BillingInterval.Monthly => currentPeriod.PeriodEndUtc.AddMonths(1),
            BillingInterval.Yearly => currentPeriod.PeriodEndUtc.AddYears(1),
            _ => throw new AggregateStateException("The billing interval is not supported for renewal.")
        };

        return new SubscriptionRenewal(
            subscription.Id,
            subscription.TenantId,
            subscription.BillingAccountId,
            currentPeriod.PeriodEndUtc,
            targetEndUtc,
            currentPeriod.Price,
            currentPeriod.CurrencyId,
            currentPeriod.BillingInterval,
            createdAtUtc);
    }

    public void MarkAwaitingAuthorization() => Status = SubscriptionRenewalStatus.AwaitingAuthorization;

    public bool MarkReminderSent(int sequence)
    {
        if (sequence <= ReminderSequenceSent || Status is SubscriptionRenewalStatus.Fulfilled or SubscriptionRenewalStatus.Cancelled)
        {
            return false;
        }

        ReminderSequenceSent = sequence;
        return true;
    }

    public void MarkCancelled(DateTimeOffset completedAtUtc)
    {
        Status = SubscriptionRenewalStatus.Cancelled;
        CompletedAtUtc = completedAtUtc;
        NextRetryAtUtc = null;
    }
}
