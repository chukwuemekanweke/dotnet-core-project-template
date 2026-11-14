using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;

public sealed class CancelSubscriptionAtPeriodEndHandler(
    IRepository<BillingAccount> billingAccountRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CancelSubscriptionAtPeriodEndResult> HandleAsync(
        CancelSubscriptionAtPeriodEndCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required.");
        var billingAccount = await billingAccountRepository.FirstOrDefaultAsync(
            new BillingAccountByOwnerSpecification(
                tenantId,
                command.BillingAccountOwnerType,
                command.BillingAccountOwnerType == BillingAccountOwnerType.Stakeholder ? stakeholderId : null),
            cancellationToken);
        if (billingAccount is null)
        {
            return new CancelSubscriptionAtPeriodEndResult(false, false, null);
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new ActiveSubscriptionByBillingAccountSpecification(tenantId, billingAccount.Id),
            cancellationToken);
        if (subscription is null)
        {
            return new CancelSubscriptionAtPeriodEndResult(false, false, null);
        }

        var operationId = Guid.CreateVersion7();
        var changed = subscription.ScheduleCancellation(
            timeProvider.GetUtcNow(),
            operationId,
            stakeholderId.ToString());
        if (changed)
        {
            var statusHistory = subscription.StatusHistory.Single(history => history.OperationId == operationId);
            await lifecyclePersistence.AddStatusHistoryAsync(statusHistory, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.CancellationScheduled,
                ObservabilityEventProperties.Create(
                    command.ActorContext,
                    stakeholderId,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                        [Observability.PropertyNames.Subscriptions.BillingAccountId] = billingAccount.Id.ToString()
                    }));
        }

        return new CancelSubscriptionAtPeriodEndResult(true, changed, subscription.CurrentPeriodEndUtc);
    }
}
