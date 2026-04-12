using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;

namespace BackendProjectTemplate.Infrastructure.Messaging;

internal sealed class CommandSender(
    IOutboxWriter outboxWriter,
    ICurrentActor currentActor) : ICommandSender
{
    public Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand
    {
        message.ActorId = currentActor.ActorId;
        if (message.TenantId == Guid.Empty && currentActor.TenantId.HasValue)
        {
            message.TenantId = currentActor.TenantId.Value;
        }

        message.CorrelationId = currentActor.CorrelationId;

        return outboxWriter.AddCommandAsync(message, cancellationToken);
    }
}
