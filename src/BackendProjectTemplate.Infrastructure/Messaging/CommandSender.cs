using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Domain.Common.Messaging;

namespace BackendProjectTemplate.Infrastructure.Messaging;

internal sealed class CommandSender(IOutboxWriter outboxWriter) : ICommandSender
{
    public Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand =>
        outboxWriter.AddCommandAsync(message, cancellationToken);
}
