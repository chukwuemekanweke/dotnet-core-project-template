using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Microsoft.Extensions.Logging;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;

/// <summary>
/// Terminates entitlement now for an explicitly targeted subscription. Open (not yet charging) renewals are
/// cancelled in the same transaction; any still-scheduled activation, renewal, reminder or expiry command
/// becomes a stale no-op because every consumer re-reads the subscription state. Periods and payment
/// records are retained and no refund is implied.
/// </summary>
public sealed class CancelSubscriptionImmediatelyHandler(
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionRenewal> renewalRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CancelSubscriptionImmediatelyHandler> logger)
{
    public async Task<CancelSubscriptionImmediatelyResult> HandleAsync(
        CancelSubscriptionImmediatelyCommand command,
        CancellationToken cancellationToken)
    {
        var actorId = command.ActorContext.StakeholderId?.ToString()
            ?? throw new InvalidOperationException("An authenticated administrator is required to cancel a subscription.");
        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(command.SubscriptionId),
            cancellationToken);
        if (subscription is null || subscription.TenantId != command.TenantId)
        {
            return new CancelSubscriptionImmediatelyResult(
                CancelSubscriptionImmediatelyStatus.NotFound,
                null,
                $"Subscription '{command.SubscriptionId}' was not found for tenant '{command.TenantId}'.");
        }

        var now = timeProvider.GetUtcNow();
        var operationId = Guid.CreateVersion7();
        bool changed;
        try
        {
            changed = subscription.CancelImmediately(now, operationId, actorId, command.Reason);
        }
        catch (AggregateStateException exception)
        {
            return new CancelSubscriptionImmediatelyResult(CancelSubscriptionImmediatelyStatus.InvalidState, null, exception.Message);
        }

        if (!changed)
        {
            return new CancelSubscriptionImmediatelyResult(
                CancelSubscriptionImmediatelyStatus.AlreadyCancelled,
                AdminSubscriptionLifecycleMapper.ToResponse(subscription),
                null);
        }

        await lifecyclePersistence.AddStatusHistoryAsync(
            subscription.StatusHistory.Single(history => history.OperationId == operationId),
            cancellationToken);
        var openRenewals = await renewalRepository.ListAsync(
            new OpenSubscriptionRenewalsBySubscriptionSpecification(subscription.Id),
            cancellationToken);
        foreach (var renewal in openRenewals)
        {
            renewal.MarkCancelled(now);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionConcurrencyConflictException exception)
        {
            return new CancelSubscriptionImmediatelyResult(CancelSubscriptionImmediatelyStatus.ConcurrencyConflict, null, exception.Message);
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.CancelledImmediately,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                command.ActorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString(),
                    [Observability.PropertyNames.Subscriptions.OperationId] = operationId.ToString()
                }));
        logger.LogInformation(
            "Administrator {ActorId} cancelled subscription {SubscriptionId} immediately; {CancelledRenewals} open renewals were cancelled.",
            actorId,
            subscription.Id,
            openRenewals.Count);

        return new CancelSubscriptionImmediatelyResult(
            CancelSubscriptionImmediatelyStatus.Cancelled,
            AdminSubscriptionLifecycleMapper.ToResponse(subscription),
            null);
    }
}
