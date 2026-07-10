using System.Diagnostics;
using BackendProjectTemplate.Contracts.Common;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Messaging.Specifications;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<MessageInbox>? messageInboxRepository,
    IUnitOfWork? unitOfWork,
    TimeProvider timeProvider) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    protected BaseMessageHandler(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : this(customTelemetryContext, currentActorAccessor, messageContext, null, null!, TimeProvider.System)
    {
    }

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.MessageType, typeof(TMessage).Name);
        var messageId = GetMessageId(message);
        var messageType = GetMessageType();

        if (message is BaseEvent baseEvent)
        {
            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            var tenantId = baseEvent.TenantId == Guid.Empty ? (Guid?)null : baseEvent.TenantId;
            currentActorAccessor.Set(
                baseEvent.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                tenantId,
                messageContext.CorrelationId,
                baseEvent.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseEvent.MessageId.ToString())
                .SetProperty("OccurredAt", baseEvent.OccuredAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseEvent.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.TenantId, tenantId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseEvent.FlowId ?? string.Empty);
        }
        else if (message is BaseCommand baseCommand)
        {
            if (baseCommand.TenantId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid tenant id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseCommand.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseCommand.TenantId,
                messageContext.CorrelationId,
                baseCommand.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseCommand.MessageId.ToString())
                .SetProperty("RequestedAt", baseCommand.RequestedAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseCommand.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.TenantId, baseCommand.TenantId.ToString())
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseCommand.FlowId ?? string.Empty);
        }
        else
        {
            throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.");
        }

        if (messageInboxRepository is not null)
        {
            var existingInbox = await messageInboxRepository.FirstOrDefaultAsync(
                new MessageInboxByMessageIdAndTypeSpecification(messageId, messageType),
                cancellationToken);
            if (existingInbox is not null)
            {
                return;
            }
        }

        foreach (var telemetryParameter in GetTelemetryParameters(message))
        {
            CustomTelemetryContext.SetProperty(telemetryParameter.Key, telemetryParameter.Value);
        }

        await HandleAsyncInternal(message, cancellationToken);

        if (messageInboxRepository is not null)
        {
            var inbox = MessageInbox.Create(messageId, messageType, timeProvider.GetUtcNow());
            await messageInboxRepository.AddAsync(inbox, cancellationToken);
            if (unitOfWork is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(IUnitOfWork)} is required when message inbox tracking is enabled.");
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    protected virtual IEnumerable<(string Key, string Value)> GetTelemetryParameters(TMessage message) => [];

    protected abstract Task HandleAsyncInternal(TMessage message, CancellationToken cancellationToken);

    private static Guid GetMessageId(TMessage message) =>
        message switch
        {
            BaseEvent baseEvent => baseEvent.MessageId,
            BaseCommand baseCommand => baseCommand.MessageId,
            _ => throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.")
        };

    private static string GetMessageType()
    {
        var type = typeof(TMessage);
        return type.FullName ?? type.Name;
    }
}
