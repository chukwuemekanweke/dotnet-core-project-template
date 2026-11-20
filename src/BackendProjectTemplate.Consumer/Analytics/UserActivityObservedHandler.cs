using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Analytics;

public sealed class UserActivityObservedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IUserDailyActivityPersistence activityPersistence,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<UserActivityObservedHandler> logger)
    : BaseMessageHandler<UserActivityObserved>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override Task HandleAsyncInternal(
        UserActivityObserved message,
        CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue
            || message.TenantId == Guid.Empty
            || string.IsNullOrWhiteSpace(message.ActivityType))
        {
            throw new CannotProcessMessageNonTransientException(
                "UserActivityObserved must contain valid tenant and stakeholder identifiers.");
        }

        return activityPersistence.ObserveAsync(
            message.MessageId,
            message.TenantId,
            message.StakeholderId.Value,
            message.ActivityDate,
            message.OccuredAt,
            message.ActivityType,
            cancellationToken);
    }
}
