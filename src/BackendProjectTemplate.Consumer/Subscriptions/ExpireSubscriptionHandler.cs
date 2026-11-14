using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class ExpireSubscriptionHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ExpireSubscriptionHandler> logger) : BaseMessageHandler<ExpireSubscriptionCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(ExpireSubscriptionCommand message, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(message.SubscriptionId),
            cancellationToken);
        var now = Clock.GetUtcNow();
        if (subscription is null ||
            subscription.TenantId != message.TenantId ||
            subscription.Status != SubscriptionStatus.Active ||
            subscription.CurrentPeriodEndUtc != message.ExpectedPeriodEndUtc ||
            now < message.ExpectedPeriodEndUtc)
        {
            return;
        }

        subscription.End(now, message.MessageId);
        var history = subscription.StatusHistory.Single(item => item.OperationId == message.MessageId);
        await lifecyclePersistence.AddStatusHistoryAsync(history, cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.Expired,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString()
                }));
    }
}
