using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class Subscription : Entity, IAggregateRoot
{
    private readonly List<SubscriptionPeriod> _periods = [];
    private readonly List<SubscriptionStatusHistory> _statusHistory = [];

    private Subscription()
    {
    }

    private Subscription(
        Guid tenantId,
        Guid billingAccountId,
        Guid planId,
        bool autoRenew,
        DateTimeOffset createdAtUtc,
        Guid operationId,
        string? actorId)
    {
        TenantId = RequireId(tenantId, nameof(tenantId));
        BillingAccountId = RequireId(billingAccountId, nameof(billingAccountId));
        PlanId = RequireId(planId, nameof(planId));
        Status = SubscriptionStatus.Pending;
        AutoRenew = autoRenew;
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            null,
            Status,
            SubscriptionLifecycleReasons.PurchaseInitiated,
            createdAtUtc,
            actorId,
            operationId,
            null));
    }

    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset? CurrentPeriodStartUtc { get; private set; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; private set; }
    public DateTimeOffset? NextBillingAtUtc { get; private set; }
    public bool AutoRenew { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTimeOffset? CancellationRequestedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public Guid? LastSuccessfulPaymentTransactionId { get; private set; }
    public uint RowVersion { get; private set; }
    public IReadOnlyCollection<SubscriptionPeriod> Periods => _periods;
    public IReadOnlyCollection<SubscriptionStatusHistory> StatusHistory => _statusHistory;

    public static Subscription CreatePending(
        Guid tenantId,
        Guid billingAccountId,
        Guid planId,
        bool autoRenew,
        DateTimeOffset createdAtUtc,
        Guid operationId,
        string? actorId) =>
        new(tenantId, billingAccountId, planId, autoRenew, createdAtUtc, operationId, actorId);

    public SubscriptionPeriod ActivateFromPayment(
        SubscriptionPurchase purchase,
        Guid paymentTransactionId,
        DateTimeOffset activatedAtUtc,
        Guid operationId)
    {
        if (paymentTransactionId == Guid.Empty)
        {
            throw new ArgumentException("Payment transaction id is required.", nameof(paymentTransactionId));
        }

        var duplicatePeriod = _periods.SingleOrDefault(period => period.PaymentTransactionId == paymentTransactionId);
        if (duplicatePeriod is not null)
        {
            return duplicatePeriod;
        }

        if (Status != SubscriptionStatus.Pending)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot activate from status '{Status}'.");
        }

        if (purchase.PlanId != PlanId || purchase.SubscriptionId != Id)
        {
            throw new AggregateStateException($"Purchase '{purchase.Id}' does not match subscription '{Id}'.");
        }

        var previousStatus = Status;
        var periodEndUtc = purchase.BillingInterval switch
        {
            BillingInterval.Monthly => activatedAtUtc.AddMonths(1),
            BillingInterval.Yearly => activatedAtUtc.AddYears(1),
            _ => throw new AggregateStateException($"Purchase '{purchase.Id}' has an unsupported billing interval.")
        };
        var period = new SubscriptionPeriod(
            Id,
            activatedAtUtc,
            periodEndUtc,
            purchase.PlanId,
            purchase.PlanCode,
            purchase.ExpectedAmount,
            purchase.ExpectedCurrencyId,
            purchase.BillingInterval,
            paymentTransactionId,
            SubscriptionActivationSource.PaidPurchase,
            operationId);

        _periods.Add(period);
        Status = SubscriptionStatus.Active;
        CurrentPeriodStartUtc = activatedAtUtc;
        CurrentPeriodEndUtc = periodEndUtc;
        NextBillingAtUtc = AutoRenew ? periodEndUtc : null;
        LastSuccessfulPaymentTransactionId = paymentTransactionId;
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            previousStatus,
            Status,
            SubscriptionLifecycleReasons.InitialPaymentConfirmed,
            activatedAtUtc,
            null,
            operationId,
            paymentTransactionId));

        return period;
    }

    public bool ScheduleCancellation(DateTimeOffset requestedAtUtc, Guid operationId, string actorId)
    {
        if (Status != SubscriptionStatus.Active)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot schedule cancellation from status '{Status}'.");
        }

        if (CancelAtPeriodEnd)
        {
            return false;
        }

        CancelAtPeriodEnd = true;
        AutoRenew = false;
        NextBillingAtUtc = null;
        CancellationRequestedAtUtc = requestedAtUtc;
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            Status,
            Status,
            SubscriptionLifecycleReasons.CancellationScheduled,
            requestedAtUtc,
            actorId,
            operationId,
            null));
        return true;
    }

    public SubscriptionPeriod ActivateAdministratively(
        SubscriptionPlan plan,
        DateTimeOffset activatedAtUtc,
        Guid operationId,
        string actorId)
    {
        if (Status != SubscriptionStatus.Pending || plan.Id != PlanId)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot be administratively activated.");
        }

        var periodEndUtc = plan.CalculatePeriodEnd(activatedAtUtc);
        var period = new SubscriptionPeriod(
            Id,
            activatedAtUtc,
            periodEndUtc,
            plan.Id,
            plan.Code,
            plan.Price,
            plan.CurrencyId,
            plan.BillingInterval,
            null,
            SubscriptionActivationSource.AdministrativeAllocation,
            operationId);
        _periods.Add(period);
        var previousStatus = Status;
        Status = SubscriptionStatus.Active;
        CurrentPeriodStartUtc = activatedAtUtc;
        CurrentPeriodEndUtc = periodEndUtc;
        NextBillingAtUtc = AutoRenew ? periodEndUtc : null;
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            previousStatus,
            Status,
            SubscriptionLifecycleReasons.AdministrativeAllocation,
            activatedAtUtc,
            actorId,
            operationId,
            null));
        return period;
    }

    public void End(DateTimeOffset endedAtUtc, Guid operationId)
    {
        if (Status != SubscriptionStatus.Active || !CurrentPeriodEndUtc.HasValue || endedAtUtc < CurrentPeriodEndUtc.Value)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot end before its active period has elapsed.");
        }

        var previousStatus = Status;
        Status = CancelAtPeriodEnd ? SubscriptionStatus.Cancelled : SubscriptionStatus.Expired;
        EndedAtUtc = endedAtUtc;
        CancelledAtUtc = CancelAtPeriodEnd ? endedAtUtc : null;
        NextBillingAtUtc = null;
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            previousStatus,
            Status,
            SubscriptionLifecycleReasons.PeriodEnded,
            endedAtUtc,
            null,
            operationId,
            null));
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value != Guid.Empty ? value : throw new ArgumentException($"{parameterName} is required.", parameterName);
}
