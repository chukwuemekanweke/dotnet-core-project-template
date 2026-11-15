using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;

public sealed class AdministrativelyCancelSubscriptionAtPeriodEndHandler(
    IRepository<SubscriptionAggregate> subscriptionRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICommandSender commandSender,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AdministrativelyCancelSubscriptionAtPeriodEndResult> HandleAsync(
        AdministrativelyCancelSubscriptionAtPeriodEndCommand command,
        CancellationToken cancellationToken)
    {
        var actorId = command.ActorContext.StakeholderId?.ToString()
            ?? throw new InvalidOperationException("An authenticated administrator is required to cancel a subscription.");
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An administrative reason is required.", nameof(command));
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(command.SubscriptionId),
            cancellationToken);
        if (subscription is null || subscription.TenantId != command.TenantId)
        {
            return new AdministrativelyCancelSubscriptionAtPeriodEndResult(
                AdministrativelyCancelSubscriptionAtPeriodEndStatus.NotFound,
                null,
                $"Subscription '{command.SubscriptionId}' was not found for tenant '{command.TenantId}'.");
        }

        var operationId = Guid.CreateVersion7();
        bool changed;
        try
        {
            changed = subscription.ScheduleCancellation(timeProvider.GetUtcNow(), operationId, actorId, reason);
        }
        catch (AggregateStateException exception)
        {
            return new AdministrativelyCancelSubscriptionAtPeriodEndResult(
                AdministrativelyCancelSubscriptionAtPeriodEndStatus.InvalidState,
                null,
                exception.Message);
        }

        if (!changed)
        {
            return new AdministrativelyCancelSubscriptionAtPeriodEndResult(
                AdministrativelyCancelSubscriptionAtPeriodEndStatus.AlreadyScheduled,
                AdminSubscriptionLifecycleMapper.ToResponse(subscription),
                null);
        }

        await lifecyclePersistence.AddStatusHistoryAsync(
            subscription.StatusHistory.Single(history => history.OperationId == operationId),
            cancellationToken);
        if (subscription.CurrentPeriodEndUtc.HasValue)
        {
            await commandSender.ScheduleSendAsync(
                new ExpireSubscriptionCommand(subscription.Id, subscription.CurrentPeriodEndUtc.Value)
                {
                    TenantId = subscription.TenantId
                },
                subscription.CurrentPeriodEndUtc.Value,
                cancellationToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionConcurrencyConflictException exception)
        {
            return new AdministrativelyCancelSubscriptionAtPeriodEndResult(
                AdministrativelyCancelSubscriptionAtPeriodEndStatus.ConcurrencyConflict,
                null,
                exception.Message);
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.AdministrativeCancellationScheduled,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                command.ActorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString(),
                    [Observability.PropertyNames.Subscriptions.OperationId] = operationId.ToString()
                }));

        return new AdministrativelyCancelSubscriptionAtPeriodEndResult(
            AdministrativelyCancelSubscriptionAtPeriodEndStatus.Scheduled,
            AdminSubscriptionLifecycleMapper.ToResponse(subscription),
            null);
    }
}
