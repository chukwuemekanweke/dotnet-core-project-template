using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

/// <summary>
/// Durable, immutable audit of an administrative entitlement grant: why, by whom and for which window a
/// complimentary subscription was granted. It is distinct from <see cref="SubscriptionStatusHistory"/>
/// (lifecycle transitions) and from financial records: an allocation never implies collected money. The plan
/// terms are snapshotted so a scheduled grant activates on the terms that were approved, even if the plan is
/// later repriced or deactivated. <see cref="OperationId"/> is the caller-supplied idempotency key and
/// correlates the allocation with the subscription period and status history it produced.
/// </summary>
public sealed class SubscriptionAllocation : Entity, IAggregateRoot
{
    public const int MaxReasonLength = 1000;
    public const int MaxActorIdLength = SubscriptionStatusHistory.MaxActorIdLength;
    public const int MaxRequestFingerprintLength = 64;

    private SubscriptionAllocation()
    {
    }

    private SubscriptionAllocation(
        Subscription subscription,
        SubscriptionPlan plan,
        SubscriptionAllocationSource allocationSource,
        string reason,
        string allocatedByActorId,
        Guid operationId,
        string requestFingerprint)
    {
        if (!Enum.IsDefined(allocationSource))
        {
            throw new ArgumentOutOfRangeException(nameof(allocationSource), allocationSource, "Allocation source is invalid.");
        }

        if (subscription.PlanId != plan.Id ||
            !subscription.EntitlementStartUtc.HasValue ||
            !subscription.EntitlementEndUtc.HasValue ||
            subscription.AutoRenew)
        {
            throw new ArgumentException("The subscription is not an administrative allocation for this plan.", nameof(subscription));
        }

        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation id is required.", nameof(operationId));
        }

        SubscriptionId = subscription.Id;
        TenantId = subscription.TenantId;
        BillingAccountId = subscription.BillingAccountId;
        PlanId = plan.Id;
        PlanCode = plan.Code;
        NominalPrice = plan.GetPrice(subscription.BillingInterval);
        CurrencyId = plan.CurrencyId;
        BillingInterval = subscription.BillingInterval;
        StartAtUtc = subscription.EntitlementStartUtc.Value;
        EndAtUtc = subscription.EntitlementEndUtc.Value;
        AllocationSource = allocationSource;
        Reason = NormalizeReason(reason);
        AllocatedByActorId = NormalizeActor(allocatedByActorId);
        OperationId = operationId;
        RequestFingerprint = string.IsNullOrWhiteSpace(requestFingerprint) || requestFingerprint.Length > MaxRequestFingerprintLength
            ? throw new ArgumentException("A request fingerprint of at most 64 characters is required.", nameof(requestFingerprint))
            : requestFingerprint;
    }

    public Guid SubscriptionId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid PlanId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;

    /// <summary>Reference price of the plan at allocation time. Never money collected.</summary>
    public decimal NominalPrice { get; private set; }
    public Guid CurrencyId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public SubscriptionAllocationSource AllocationSource { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string AllocatedByActorId { get; private set; } = string.Empty;
    public Guid OperationId { get; private set; }

    /// <summary>
    /// Hash of the complete canonical allocation request. A replay with the same <see cref="OperationId"/> is
    /// only treated as idempotent when its fingerprint matches; otherwise the key was reused for a different grant.
    /// </summary>
    public string RequestFingerprint { get; private set; } = string.Empty;

    public static SubscriptionAllocation Create(
        Subscription subscription,
        SubscriptionPlan plan,
        SubscriptionAllocationSource allocationSource,
        string reason,
        string allocatedByActorId,
        Guid operationId,
        string requestFingerprint)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(plan);
        return new SubscriptionAllocation(subscription, plan, allocationSource, reason, allocatedByActorId, operationId, requestFingerprint);
    }

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An administrative reason is required.", nameof(reason));
        }

        var normalized = reason.Trim();
        return normalized.Length <= MaxReasonLength
            ? normalized
            : throw new ArgumentException($"Reason must not exceed {MaxReasonLength} characters.", nameof(reason));
    }

    private static string NormalizeActor(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("An authenticated actor is required.", nameof(actorId));
        }

        var normalized = actorId.Trim();
        return normalized.Length <= MaxActorIdLength
            ? normalized
            : throw new ArgumentException($"Actor id must not exceed {MaxActorIdLength} characters.", nameof(actorId));
    }
}
