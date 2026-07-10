using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserEmailConfirmedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UserEmailConfirmedHandler> logger,
    IRepository<MessageInbox> messageInboxRepository) : BaseMessageHandler<UserEmailConfirmed>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider)
{
    protected override Task HandleAsyncInternal(UserEmailConfirmed message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processed user email confirmed event for stakeholder {StakeholderId}.",
            message.StakeholderId);

        if (message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, message.StakeholderId.Value.ToString());
        }

        return Task.CompletedTask;
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserEmailConfirmed message)
    {
        yield break;
    }
}
