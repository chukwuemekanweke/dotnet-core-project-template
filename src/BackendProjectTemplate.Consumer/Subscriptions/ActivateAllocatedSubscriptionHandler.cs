using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Subscriptions;

/// <summary>
/// Activates a future-dated administrative allocation once its entitlement start arrives. The handler always
/// re-reads the authoritative subscription and allocation, so duplicate, stale and late deliveries are safe:
/// anything other than a still-Scheduled subscription whose tenant, BillingAccount and window match the
/// command is a no-op. Early delivery re-schedules itself; late delivery activates from the agreed start so
/// the grant is never extended; delivery after the whole window has elapsed lapses the allocation straight to
/// Expired without ever granting entitlement. Plan deactivation after approval does not block activation
/// because the allocation carries its own plan snapshot. The activation is saved here so an entitlement
/// conflict detected by PostgreSQL is resolved deterministically instead of relying on broker redelivery.
/// </summary>
public sealed class ActivateAllocatedSubscriptionHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionAllocation> allocationRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ActivateAllocatedSubscriptionHandler> logger) : BaseMessageHandler<ActivateAllocatedSubscriptionCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<ActivateAllocatedSubscriptionHandler> _logger = logger;

    protected override async Task HandleAsyncInternal(
        ActivateAllocatedSubscriptionCommand message,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(message.SubscriptionId),
            cancellationToken);
        var allocation = await allocationRepository.GetByIdAsync(message.AllocationId, cancellationToken);
        if (subscription is null ||
            allocation is null ||
            subscription.TenantId != message.TenantId ||
            allocation.TenantId != subscription.TenantId ||
            allocation.SubscriptionId != subscription.Id ||
            allocation.BillingAccountId != subscription.BillingAccountId)
        {
            _logger.LogWarning(
                "Ignored allocation activation for subscription {SubscriptionId}: subscription or allocation identity does not match.",
                message.SubscriptionId);
            return;
        }

        if (subscription.Status != SubscriptionStatus.Scheduled ||
            subscription.EntitlementStartUtc != message.ExpectedStartUtc ||
            allocation.StartAtUtc != message.ExpectedStartUtc)
        {
            _logger.LogInformation(
                "Ignored stale allocation activation for subscription {SubscriptionId} in status {Status}.",
                subscription.Id,
                subscription.Status);
            return;
        }

        var now = Clock.GetUtcNow();
        if (now < allocation.StartAtUtc)
        {
            await commandSender.ScheduleSendAsync(
                new ActivateAllocatedSubscriptionCommand(subscription.Id, allocation.Id, allocation.StartAtUtc)
                {
                    TenantId = subscription.TenantId,
                    StakeholderId = message.StakeholderId
                },
                allocation.StartAtUtc,
                cancellationToken);
            return;
        }

        if (now >= allocation.EndAtUtc)
        {
            subscription.LapseScheduledAllocation(allocation, now, message.MessageId);
            await lifecyclePersistence.AddStatusHistoryAsync(
                subscription.StatusHistory.Single(history => history.OperationId == message.MessageId),
                cancellationToken);
            _logger.LogWarning(
                "Allocation {AllocationId} for subscription {SubscriptionId} lapsed without activation because its window ended at {EndAtUtc}.",
                allocation.Id,
                subscription.Id,
                allocation.EndAtUtc);
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.AllocationLapsed,
                ObservabilityEventProperties.Create(
                    ActorAccessor,
                    message.StakeholderId,
                    additionalProperties: CreateProperties(subscription, allocation)));
            return;
        }

        // Defense in depth: a Scheduled window is already covered by the PostgreSQL exclusion constraint, so a
        // conflict here indicates data repaired outside the domain. Leave the grant Scheduled for review.
        if (await subscriptionRepository.AnyAsync(
                new OverlappingEntitlementSpecification(
                    subscription.TenantId,
                    subscription.BillingAccountId,
                    allocation.StartAtUtc,
                    allocation.EndAtUtc,
                    subscription.Id),
                cancellationToken))
        {
            _logger.LogError(
                "Allocation {AllocationId} for subscription {SubscriptionId} was not activated because billing account {BillingAccountId} holds a conflicting entitlement.",
                allocation.Id,
                subscription.Id,
                subscription.BillingAccountId);
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.AllocationActivationSkipped,
                ObservabilityEventProperties.Create(
                    ActorAccessor,
                    message.StakeholderId,
                    failureReason: SubscriptionFulfillmentBlockReasons.EntitlementConflict,
                    additionalProperties: CreateProperties(subscription, allocation)));
            return;
        }

        var period = subscription.ActivateScheduledAllocation(allocation, now, message.MessageId);
        await lifecyclePersistence.AddPeriodAsync(period, cancellationToken);
        await lifecyclePersistence.AddStatusHistoryAsync(
            subscription.StatusHistory.Single(history => history.OperationId == message.MessageId),
            cancellationToken);
        await commandSender.ScheduleSendAsync(
            new ExpireSubscriptionCommand(subscription.Id, period.PeriodEndUtc)
            {
                TenantId = subscription.TenantId,
                StakeholderId = message.StakeholderId
            },
            period.PeriodEndUtc,
            cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionEntitlementConflictException)
        {
            _unitOfWork.DiscardChanges();
            _logger.LogError(
                "Allocation {AllocationId} for subscription {SubscriptionId} was not activated because a concurrent entitlement was committed on billing account {BillingAccountId}.",
                allocation.Id,
                subscription.Id,
                subscription.BillingAccountId);
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.AllocationActivationSkipped,
                ObservabilityEventProperties.Create(
                    ActorAccessor,
                    message.StakeholderId,
                    failureReason: SubscriptionFulfillmentBlockReasons.EntitlementConflict,
                    additionalProperties: CreateProperties(subscription, allocation)));
            return;
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.AllocationActivated,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                message.StakeholderId,
                additionalProperties: CreateProperties(subscription, allocation)));
    }

    private static Dictionary<string, string> CreateProperties(
        SubscriptionAggregate subscription,
        SubscriptionAllocation allocation) =>
        new()
        {
            [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
            [Observability.PropertyNames.Subscriptions.AllocationId] = allocation.Id.ToString(),
            [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString()
        };
}
