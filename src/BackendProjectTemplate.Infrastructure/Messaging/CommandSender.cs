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
        if (!message.StakeholderId.HasValue && Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            message.StakeholderId = stakeholderId;
        }

        if (string.IsNullOrWhiteSpace(message.FlowId))
        {
            message.FlowId = currentActor.FlowId;
        }

        if (message.TenantId == Guid.Empty && currentActor.TenantId.HasValue)
        {
            message.TenantId = currentActor.TenantId.Value;
        }

        return outboxWriter.AddCommandAsync(message, cancellationToken);
    }
}
