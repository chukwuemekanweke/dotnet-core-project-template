using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class Subscription : Entity, IAggregateRoot
{
    public const int MaxCancellationReasonLength = 1000;

    private readonly List<SubscriptionPeriod> _periods = [];
    private readonly List<SubscriptionStatusHistory> _statusHistory = [];

    private Subscription()
    {
    }

    private Subscription(
        Guid tenantId,
        Guid billingAccountId,
        Guid planId,
        bool autoRenew)
    {
        TenantId = RequireId(tenantId, nameof(tenantId));
        BillingAccountId = RequireId(billingAccountId, nameof(billingAccountId));
        PlanId = RequireId(planId, nameof(planId));
        AutoRenew = autoRenew;
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
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    /// <summary>
    /// Inclusive start of the entitlement window reserved or held by this subscription. Together with
    /// <see cref="EntitlementEndUtc"/> it forms the half-open interval <c>[start, end)</c> that PostgreSQL
    /// uses to exclude overlapping Scheduled/Active entitlements on the same BillingAccount.
    /// </summary>
    public DateTimeOffset? EntitlementStartUtc { get; private set; }

    /// <summary>
    /// Exclusive end of the entitlement window. <c>null</c> means open-ended: a paid subscription that will
    /// keep renewing automatically holds the BillingAccount's entitlement until it is cancelled or ends.
    /// </summary>
    public DateTimeOffset? EntitlementEndUtc { get; private set; }

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
        string? actorId)
    {
        var subscription = new Subscription(tenantId, billingAccountId, planId, autoRenew)
        {
            Status = SubscriptionStatus.Pending
        };
        subscription.RecordTransition(
            null,
            SubscriptionLifecycleReasons.PurchaseInitiated,
            createdAtUtc,
            actorId,
            operationId,
            null,
            null);
        return subscription;
    }

    /// <summary>
    /// Creates a complimentary, non-renewing subscription granted by an administrator. When the entitlement
    /// starts at or before <paramref name="allocatedAtUtc"/> it is activated immediately; otherwise it is
    /// <see cref="SubscriptionStatus.Scheduled"/> and reserves its future window until a scheduled activation
    /// command activates it. No payment, purchase or renewal identity is ever associated with it.
    /// </summary>
    public static Subscription CreateAllocated(
        Guid tenantId,
        Guid billingAccountId,
        SubscriptionPlan plan,
        DateTimeOffset entitlementStartUtc,
        DateTimeOffset entitlementEndUtc,
        DateTimeOffset allocatedAtUtc,
        Guid operationId,
        string actorId,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(plan);
        RequireActor(actorId);
        reason = NormalizeRequiredReason(reason);
        entitlementStartUtc = SubscriptionTimestamps.Normalize(entitlementStartUtc);
        entitlementEndUtc = SubscriptionTimestamps.Normalize(entitlementEndUtc);
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation id is required.", nameof(operationId));
        }

        if (entitlementEndUtc <= entitlementStartUtc)
        {
            throw new ArgumentException("Entitlement end must be after its start.", nameof(entitlementEndUtc));
        }

        if (entitlementEndUtc <= allocatedAtUtc)
        {
            throw new ArgumentException("Entitlement end must be in the future.", nameof(entitlementEndUtc));
        }

        var subscription = new Subscription(tenantId, billingAccountId, plan.Id, autoRenew: false)
        {
            EntitlementStartUtc = entitlementStartUtc,
            EntitlementEndUtc = entitlementEndUtc
        };

        if (entitlementStartUtc > allocatedAtUtc)
        {
            subscription.Status = SubscriptionStatus.Scheduled;
            subscription.RecordTransition(
                null,
                SubscriptionLifecycleReasons.AllocationScheduled,
                allocatedAtUtc,
                actorId,
                operationId,
                null,
                reason);
            return subscription;
        }

        subscription.StartAdministrativePeriod(
            null,
            entitlementStartUtc,
            entitlementEndUtc,
            plan.Id,
            plan.Code,
            plan.Price,
            plan.CurrencyId,
            plan.BillingInterval,
            operationId,
            allocatedAtUtc,
            operationId,
            actorId,
            SubscriptionLifecycleReasons.AdministrativeAllocation,
            reason);
        return subscription;
    }

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

        var duplicatePeriod = FindPeriodForPayment(paymentTransactionId);
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

        activatedAtUtc = SubscriptionTimestamps.Normalize(activatedAtUtc);
        var previousStatus = Status;
        var periodEndUtc = BillingIntervals.CalculatePeriodEnd(purchase.BillingInterval, activatedAtUtc);
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
        EntitlementStartUtc = activatedAtUtc;
        EntitlementEndUtc = AutoRenew ? null : periodEndUtc;
        LastSuccessfulPaymentTransactionId = paymentTransactionId;
        RecordTransition(
            previousStatus,
            SubscriptionLifecycleReasons.InitialPaymentConfirmed,
            activatedAtUtc,
            null,
            operationId,
            paymentTransactionId,
            null);

        return period;
    }

    /// <summary>
    /// Returns the entitlement window a successful initial payment would claim if it were fulfilled at
    /// <paramref name="activatedAtUtc"/>, so fulfillment can be checked against the single-entitlement rule
    /// before the aggregate changes state.
    /// </summary>
    public (DateTimeOffset StartUtc, DateTimeOffset? EndUtc) ProjectPaidEntitlement(
        SubscriptionPurchase purchase,
        DateTimeOffset activatedAtUtc)
    {
        activatedAtUtc = SubscriptionTimestamps.Normalize(activatedAtUtc);
        return (activatedAtUtc, AutoRenew ? null : BillingIntervals.CalculatePeriodEnd(purchase.BillingInterval, activatedAtUtc));
    }

    public SubscriptionPeriod? FindPeriodForPayment(Guid paymentTransactionId) =>
        _periods.SingleOrDefault(period => period.PaymentTransactionId == paymentTransactionId);

    public bool ScheduleCancellation(DateTimeOffset requestedAtUtc, Guid operationId, string actorId) =>
        ScheduleCancellation(requestedAtUtc, operationId, actorId, null);

    /// <summary>
    /// Cancels at period end: entitlement is preserved through <see cref="CurrentPeriodEndUtc"/>, renewal is
    /// disabled and the decision is recorded. Repeated requests are idempotent and return <c>false</c>.
    /// </summary>
    public bool ScheduleCancellation(DateTimeOffset requestedAtUtc, Guid operationId, string actorId, string? administrativeReason)
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
        CancellationReason = NormalizeOptionalReason(administrativeReason);
        EntitlementEndUtc = CurrentPeriodEndUtc;
        RecordTransition(
            Status,
            administrativeReason is null
                ? SubscriptionLifecycleReasons.CancellationScheduled
                : SubscriptionLifecycleReasons.AdministrativeCancellationScheduled,
            requestedAtUtc,
            actorId,
            operationId,
            null,
            administrativeReason);
        return true;
    }

    /// <summary>
    /// Terminates entitlement now. Valid for Active and Scheduled subscriptions; repeated requests against an
    /// already cancelled subscription are idempotent and return <c>false</c>. Historical periods are retained:
    /// <see cref="CurrentPeriodEndUtc"/> keeps the originally granted boundary while <see cref="EndedAtUtc"/>
    /// records the effective termination.
    /// </summary>
    public bool CancelImmediately(DateTimeOffset cancelledAtUtc, Guid operationId, string actorId, string reason)
    {
        RequireActor(actorId);
        var normalizedReason = NormalizeRequiredReason(reason);
        if (Status == SubscriptionStatus.Cancelled)
        {
            return false;
        }

        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.Scheduled))
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot be cancelled immediately from status '{Status}'.");
        }

        var previousStatus = Status;
        if (previousStatus == SubscriptionStatus.Active &&
            (!EntitlementEndUtc.HasValue || EntitlementEndUtc.Value > cancelledAtUtc))
        {
            EntitlementEndUtc = EntitlementStartUtc.HasValue && cancelledAtUtc < EntitlementStartUtc.Value
                ? EntitlementStartUtc
                : cancelledAtUtc;
        }

        Status = SubscriptionStatus.Cancelled;
        AutoRenew = false;
        NextBillingAtUtc = null;
        CancellationRequestedAtUtc ??= cancelledAtUtc;
        CancellationReason = normalizedReason;
        CancelledAtUtc = cancelledAtUtc;
        EndedAtUtc = cancelledAtUtc;
        RecordTransition(
            previousStatus,
            SubscriptionLifecycleReasons.AdministrativeImmediateCancellation,
            cancelledAtUtc,
            actorId,
            operationId,
            null,
            normalizedReason);
        return true;
    }

    public SubscriptionPeriod RenewFromPayment(
        SubscriptionRenewal renewal,
        Guid paymentTransactionId,
        DateTimeOffset renewedAtUtc,
        Guid operationId)
    {
        var duplicatePeriod = FindPeriodForPayment(paymentTransactionId);
        if (duplicatePeriod is not null)
        {
            return duplicatePeriod;
        }

        if (Status != SubscriptionStatus.Active ||
            renewal.SubscriptionId != Id ||
            renewal.TenantId != TenantId ||
            renewal.BillingAccountId != BillingAccountId ||
            CurrentPeriodEndUtc != renewal.TargetPeriodStartUtc)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot be renewed for the requested billing period.");
        }

        var currentPeriod = _periods.Single(period => period.PeriodEndUtc == renewal.TargetPeriodStartUtc);
        var period = new SubscriptionPeriod(
            Id,
            renewal.TargetPeriodStartUtc,
            renewal.TargetPeriodEndUtc,
            currentPeriod.PlanId,
            currentPeriod.PlanCode,
            renewal.ExpectedPrice,
            renewal.CurrencyId,
            renewal.BillingInterval,
            paymentTransactionId,
            SubscriptionActivationSource.PaidRenewal,
            operationId);
        _periods.Add(period);
        CurrentPeriodStartUtc = period.PeriodStartUtc;
        CurrentPeriodEndUtc = period.PeriodEndUtc;
        NextBillingAtUtc = AutoRenew && !CancelAtPeriodEnd ? period.PeriodEndUtc : null;
        EntitlementEndUtc = AutoRenew && !CancelAtPeriodEnd ? null : period.PeriodEndUtc;
        LastSuccessfulPaymentTransactionId = paymentTransactionId;
        RecordTransition(
            Status,
            SubscriptionLifecycleReasons.RenewalPaymentConfirmed,
            renewedAtUtc,
            null,
            operationId,
            paymentTransactionId,
            null);
        return period;
    }

    /// <summary>
    /// Activates a <see cref="SubscriptionStatus.Scheduled"/> allocation from the allocation's immutable plan
    /// snapshot. The period starts at the agreed entitlement start (not delivery time), so late delivery does
    /// not extend the grant. Activation does not depend on the plan still being offered.
    /// </summary>
    public SubscriptionPeriod ActivateScheduledAllocation(
        SubscriptionAllocation allocation,
        DateTimeOffset activatedAtUtc,
        Guid operationId)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (Status != SubscriptionStatus.Scheduled ||
            allocation.SubscriptionId != Id ||
            allocation.TenantId != TenantId ||
            allocation.BillingAccountId != BillingAccountId ||
            allocation.PlanId != PlanId ||
            EntitlementStartUtc != allocation.StartAtUtc ||
            EntitlementEndUtc != allocation.EndAtUtc)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot activate allocation '{allocation.Id}'.");
        }

        if (activatedAtUtc < allocation.StartAtUtc)
        {
            throw new AggregateStateException(
                $"Allocation '{allocation.Id}' cannot activate before its entitlement start '{allocation.StartAtUtc:O}'.");
        }

        if (activatedAtUtc >= allocation.EndAtUtc)
        {
            throw new AggregateStateException(
                $"Allocation '{allocation.Id}' cannot activate after its entitlement ended at '{allocation.EndAtUtc:O}'.");
        }

        return StartAdministrativePeriod(
            Status,
            allocation.StartAtUtc,
            allocation.EndAtUtc,
            allocation.PlanId,
            allocation.PlanCode,
            allocation.NominalPrice,
            allocation.CurrencyId,
            allocation.BillingInterval,
            allocation.OperationId,
            activatedAtUtc,
            operationId,
            null,
            SubscriptionLifecycleReasons.ScheduledAllocationActivated,
            null);
    }

    /// <summary>
    /// Closes a <see cref="SubscriptionStatus.Scheduled"/> allocation whose whole window elapsed before it
    /// could be activated (for example a severely delayed activation command). It moves straight to
    /// <see cref="SubscriptionStatus.Expired"/> without ever granting entitlement or creating a period, and
    /// records the terminal transition.
    /// </summary>
    public void LapseScheduledAllocation(SubscriptionAllocation allocation, DateTimeOffset processedAtUtc, Guid operationId)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (Status != SubscriptionStatus.Scheduled ||
            allocation.SubscriptionId != Id ||
            EntitlementEndUtc != allocation.EndAtUtc ||
            processedAtUtc < allocation.EndAtUtc)
        {
            throw new AggregateStateException($"Subscription '{Id}' cannot lapse allocation '{allocation.Id}'.");
        }

        var previousStatus = Status;
        Status = SubscriptionStatus.Expired;
        EndedAtUtc = processedAtUtc;
        NextBillingAtUtc = null;
        RecordTransition(
            previousStatus,
            SubscriptionLifecycleReasons.ScheduledAllocationLapsed,
            processedAtUtc,
            null,
            operationId,
            null,
            null);
    }

    /// <summary>
    /// Ends a purchase whose initial payment concluded without success (failed, expired or underpaid). The
    /// subscription never held entitlement, so it moves straight from Pending to Expired and stops being the
    /// BillingAccount's current subscription. Returns <c>false</c> when it is no longer Pending.
    /// </summary>
    public bool ExpireUnpaidPurchase(Guid paymentTransactionId, DateTimeOffset expiredAtUtc, Guid operationId)
    {
        if (Status != SubscriptionStatus.Pending)
        {
            return false;
        }

        Status = SubscriptionStatus.Expired;
        EndedAtUtc = SubscriptionTimestamps.Normalize(expiredAtUtc);
        NextBillingAtUtc = null;
        RecordTransition(
            SubscriptionStatus.Pending,
            SubscriptionLifecycleReasons.InitialPaymentUnsuccessful,
            EndedAtUtc.Value,
            null,
            operationId,
            paymentTransactionId,
            null);
        return true;
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
        RecordTransition(
            previousStatus,
            SubscriptionLifecycleReasons.PeriodEnded,
            endedAtUtc,
            null,
            operationId,
            null,
            null);
    }

    private SubscriptionPeriod StartAdministrativePeriod(
        SubscriptionStatus? previousStatus,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        Guid planId,
        string planCode,
        decimal nominalPrice,
        Guid currencyId,
        BillingInterval billingInterval,
        Guid periodOperationId,
        DateTimeOffset occurredAtUtc,
        Guid historyOperationId,
        string? actorId,
        string lifecycleReason,
        string? details)
    {
        if (AutoRenew)
        {
            throw new AggregateStateException($"Administrative allocation '{Id}' cannot renew automatically.");
        }

        var period = new SubscriptionPeriod(
            Id,
            periodStartUtc,
            periodEndUtc,
            planId,
            planCode,
            nominalPrice,
            currencyId,
            billingInterval,
            null,
            SubscriptionActivationSource.AdministrativeAllocation,
            periodOperationId);
        _periods.Add(period);
        Status = SubscriptionStatus.Active;
        CurrentPeriodStartUtc = periodStartUtc;
        CurrentPeriodEndUtc = periodEndUtc;
        NextBillingAtUtc = null;
        EntitlementStartUtc = periodStartUtc;
        EntitlementEndUtc = periodEndUtc;
        RecordTransition(
            previousStatus,
            lifecycleReason,
            occurredAtUtc,
            actorId,
            historyOperationId,
            null,
            details);
        return period;
    }

    private void RecordTransition(
        SubscriptionStatus? previousStatus,
        string reason,
        DateTimeOffset occurredAtUtc,
        string? actorId,
        Guid operationId,
        Guid? paymentTransactionId,
        string? details) =>
        _statusHistory.Add(new SubscriptionStatusHistory(
            Id,
            previousStatus,
            Status,
            reason,
            occurredAtUtc,
            actorId,
            operationId,
            paymentTransactionId,
            details));

    private static string NormalizeRequiredReason(string reason) =>
        NormalizeOptionalReason(reason)
        ?? throw new ArgumentException("An administrative reason is required.", nameof(reason));

    private static string? NormalizeOptionalReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalized = reason.Trim();
        return normalized.Length <= MaxCancellationReasonLength
            ? normalized
            : throw new ArgumentException(
                $"Reason must not exceed {MaxCancellationReasonLength} characters.",
                nameof(reason));
    }

    private static void RequireActor(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("An authenticated actor is required.", nameof(actorId));
        }
    }

    private static Guid RequireId(Guid value, string parameterName) =>
        value != Guid.Empty ? value : throw new ArgumentException($"{parameterName} is required.", parameterName);
}
