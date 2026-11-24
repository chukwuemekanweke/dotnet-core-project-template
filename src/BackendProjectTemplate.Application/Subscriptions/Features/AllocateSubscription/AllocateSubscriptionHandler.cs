using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Microsoft.Extensions.Logging;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

/// <summary>
/// Grants a complimentary, non-renewing subscription to a tenant- or stakeholder-owned BillingAccount.
/// Transaction boundary: the BillingAccount is resolved (and, when new, committed idempotently by the resolver)
/// before any subscription state is tracked; the subscription, allocation audit, initial period/history and
/// the scheduled activation or expiry command then commit atomically in one SaveChanges through the outbox.
/// A retry with the same idempotency key returns the original allocation without repeating side effects.
/// </summary>
public sealed class AllocateSubscriptionHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionAllocation> allocationRepository,
    IBillingAccountResolver billingAccountResolver,
    ICommandSender commandSender,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AllocateSubscriptionHandler> logger)
{
    /// <summary>Requests up to this far in the past are treated as "start now" to tolerate clock skew.</summary>
    public static readonly TimeSpan StartClockSkewTolerance = TimeSpan.FromMinutes(5);

    public async Task<AllocateSubscriptionResult> HandleAsync(
        AllocateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var actorId = command.ActorContext.StakeholderId?.ToString()
            ?? throw new InvalidOperationException("An authenticated administrator is required to allocate a subscription.");

        var existing = await allocationRepository.FirstOrDefaultAsync(
            new SubscriptionAllocationByOperationIdSpecification(command.IdempotencyKey),
            cancellationToken);
        if (existing is not null)
        {
            return await ReplayAsync(existing, command, cancellationToken);
        }

        var plan = await planRepository.FirstOrDefaultAsync(new SubscriptionPlanByIdSpecification(command.PlanId), cancellationToken);
        if (plan is null)
        {
            return AllocateSubscriptionResult.Failed(AllocateSubscriptionStatus.PlanNotFound, $"Subscription plan '{command.PlanId}' was not found.");
        }

        if (!plan.IsActive)
        {
            return AllocateSubscriptionResult.Failed(AllocateSubscriptionStatus.PlanInactive, $"Subscription plan '{plan.Code}' is not active.");
        }

        // A plan offered on a single interval needs no choice; a plan with several requires an explicit one.
        var billingInterval = command.BillingInterval
            ?? (plan.Prices.Count == 1 ? plan.Prices.Single().BillingInterval : (BillingInterval?)null);
        if (billingInterval is null || !plan.Offers(billingInterval.Value))
        {
            return AllocateSubscriptionResult.Failed(
                AllocateSubscriptionStatus.BillingIntervalNotOffered,
                billingInterval is null
                    ? $"Subscription plan '{plan.Code}' offers several billing intervals; BillingInterval is required."
                    : $"Subscription plan '{plan.Code}' is not offered with a {billingInterval} billing interval.");
        }

        var now = SubscriptionTimestamps.Normalize(timeProvider.GetUtcNow());
        var requestedStartUtc = command.StartAtUtc.HasValue ? SubscriptionTimestamps.Normalize(command.StartAtUtc.Value) : (DateTimeOffset?)null;
        if (requestedStartUtc.HasValue && requestedStartUtc.Value < now - StartClockSkewTolerance)
        {
            return AllocateSubscriptionResult.Failed(
                AllocateSubscriptionStatus.InvalidPeriod,
                "StartAtUtc cannot be in the past; omit it to start the entitlement now.");
        }

        var startUtc = requestedStartUtc.HasValue && requestedStartUtc.Value > now ? requestedStartUtc.Value : now;
        var endUtc = command.EndAtUtc.HasValue
            ? SubscriptionTimestamps.Normalize(command.EndAtUtc.Value)
            : plan.CalculatePeriodEnd(billingInterval.Value, startUtc);
        if (endUtc <= startUtc)
        {
            return AllocateSubscriptionResult.Failed(AllocateSubscriptionStatus.InvalidPeriod, "EndAtUtc must be later than the entitlement start.");
        }

        Guid? ownerStakeholderId = command.BillingAccountOwnerType == BillingAccountOwnerType.Stakeholder
            ? command.StakeholderId
            : null;
        Domain.Payments.Entities.BillingAccount billingAccount;
        try
        {
            billingAccount = await billingAccountResolver.ResolveOrCreateAsync(
                command.TenantId,
                command.BillingAccountOwnerType,
                ownerStakeholderId,
                cancellationToken);
        }
        catch (BillingAccountOwnerNotFoundException exception)
        {
            return AllocateSubscriptionResult.Failed(AllocateSubscriptionStatus.OwnerNotFound, exception.Message);
        }
        catch (BillingAccountResolutionException exception)
        {
            return AllocateSubscriptionResult.Failed(AllocateSubscriptionStatus.OwnerTenantMismatch, exception.Message);
        }

        if (await subscriptionRepository.AnyAsync(
                new OverlappingEntitlementSpecification(command.TenantId, billingAccount.Id, startUtc, endUtc, null),
                cancellationToken))
        {
            // The initial idempotency lookup can run before a concurrent winner commits while this later
            // overlap check observes its subscription. Re-check the operation before treating it as a
            // competing entitlement so identical concurrent requests remain replay-safe.
            var winner = await allocationRepository.FirstOrDefaultAsync(
                new SubscriptionAllocationByOperationIdSpecification(command.IdempotencyKey),
                cancellationToken);
            return winner is null
                ? EntitlementConflict(billingAccount.Id)
                : await ReplayAsync(winner, command, cancellationToken);
        }

        var subscription = SubscriptionAggregate.CreateAllocated(
            command.TenantId,
            billingAccount.Id,
            plan,
            billingInterval.Value,
            startUtc,
            endUtc,
            now,
            command.IdempotencyKey,
            actorId,
            command.Reason);
        var allocation = SubscriptionAllocation.Create(
            subscription,
            plan,
            command.AllocationSource,
            command.Reason,
            actorId,
            command.IdempotencyKey,
            AllocateSubscriptionRequestFingerprint.Compute(command));

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await allocationRepository.AddAsync(allocation, cancellationToken);
        if (subscription.Status == SubscriptionStatus.Scheduled)
        {
            await commandSender.ScheduleSendAsync(
                new ActivateAllocatedSubscriptionCommand(subscription.Id, allocation.Id, allocation.StartAtUtc)
                {
                    TenantId = subscription.TenantId
                },
                allocation.StartAtUtc,
                cancellationToken);
        }
        else
        {
            await commandSender.ScheduleSendAsync(
                new ExpireSubscriptionCommand(subscription.Id, allocation.EndAtUtc)
                {
                    TenantId = subscription.TenantId
                },
                allocation.EndAtUtc,
                cancellationToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionEntitlementConflictException)
        {
            // A concurrent request with the same idempotency key inserts its subscription first, so the loser can
            // fail on the entitlement exclusion constraint before reaching the idempotency index.
            var winner = await allocationRepository.FirstOrDefaultAsync(
                new SubscriptionAllocationByOperationIdSpecification(command.IdempotencyKey),
                cancellationToken);
            return winner is null
                ? EntitlementConflict(billingAccount.Id)
                : await ReplayAsync(winner, command, cancellationToken);
        }
        catch (SubscriptionAllocationOperationConflictException)
        {
            // The losing insert stays tracked but carries different keys, so re-querying by the idempotency key
            // returns the committed winner from the database.
            var winner = await allocationRepository.FirstOrDefaultAsync(
                new SubscriptionAllocationByOperationIdSpecification(command.IdempotencyKey),
                cancellationToken)
                ?? throw new InvalidOperationException($"Allocation '{command.IdempotencyKey}' could not be reloaded after a concurrent insert.");
            return await ReplayAsync(winner, command, cancellationToken);
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.Allocated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                command.ActorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.AllocationId] = allocation.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.AllocationSource] = allocation.AllocationSource.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = billingAccount.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.PlanId] = plan.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.OperationId] = command.IdempotencyKey.ToString(),
                    [Observability.PropertyNames.Subscriptions.Status] = subscription.Status.ToString()
                }));
        logger.LogInformation(
            "Allocated subscription {SubscriptionId} ({Status}) to billing account {BillingAccountId} for tenant {TenantId} from {StartAtUtc} to {EndAtUtc}.",
            subscription.Id,
            subscription.Status,
            billingAccount.Id,
            subscription.TenantId,
            allocation.StartAtUtc,
            allocation.EndAtUtc);

        return new AllocateSubscriptionResult(
            AllocateSubscriptionStatus.Allocated,
            ToResponse(allocation, subscription.Status),
            null);
    }

    private async Task<AllocateSubscriptionResult> ReplayAsync(
        SubscriptionAllocation allocation,
        AllocateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(allocation.RequestFingerprint, AllocateSubscriptionRequestFingerprint.Compute(command), StringComparison.Ordinal))
        {
            return AllocateSubscriptionResult.Failed(
                AllocateSubscriptionStatus.IdempotencyKeyReused,
                $"Idempotency key '{command.IdempotencyKey}' was already used for a different allocation.");
        }

        var subscription = await subscriptionRepository.GetByIdAsync(allocation.SubscriptionId, cancellationToken);
        var status = subscription?.Status ?? SubscriptionStatus.Scheduled;
        return new AllocateSubscriptionResult(AllocateSubscriptionStatus.Replayed, ToResponse(allocation, status), null);
    }

    private AllocateSubscriptionResult EntitlementConflict(Guid billingAccountId)
    {
        logger.LogWarning(
            "Rejected subscription allocation for billing account {BillingAccountId} because it overlaps an existing entitlement.",
            billingAccountId);
        return AllocateSubscriptionResult.Failed(
            AllocateSubscriptionStatus.EntitlementConflict,
            "The billing account already holds or has reserved an overlapping subscription entitlement.");
    }

    private static AllocateSubscriptionResponse ToResponse(SubscriptionAllocation allocation, SubscriptionStatus status) =>
        new(
            allocation.SubscriptionId,
            allocation.Id,
            allocation.TenantId,
            allocation.BillingAccountId,
            allocation.PlanId,
            status.ToString(),
            allocation.AllocationSource.ToString(),
            allocation.StartAtUtc,
            allocation.EndAtUtc,
            false);
}
