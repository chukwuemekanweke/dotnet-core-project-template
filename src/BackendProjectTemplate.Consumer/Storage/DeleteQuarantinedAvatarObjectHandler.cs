using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Storage;

public sealed class DeleteQuarantinedAvatarObjectHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<DeleteQuarantinedAvatarObjectHandler> logger)
    : BaseMessageHandler<DeleteQuarantinedAvatarObject>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(
        DeleteQuarantinedAvatarObject message,
        CancellationToken cancellationToken)
    {
        if (message.UploadId == Guid.Empty || string.IsNullOrWhiteSpace(message.ObjectKey))
        {
            throw new CannotProcessMessageNonTransientException(
                "DeleteQuarantinedAvatarObject must contain a valid upload id and object key.");
        }

        await objectStorageService.DeletePrivateObjectAsync(message.ObjectKey, cancellationToken);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(
        DeleteQuarantinedAvatarObject message) =>
        [(Observability.PropertyNames.Common.UploadId, message.UploadId.ToString())];
}
