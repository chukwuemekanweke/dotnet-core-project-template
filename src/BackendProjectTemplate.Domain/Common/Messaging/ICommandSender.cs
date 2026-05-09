using BackendProjectTemplate.Contracts.Commands;

namespace BackendProjectTemplate.Domain.Common.Messaging;

public interface ICommandSender
{
    Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;

    Task ScheduleSendAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;
}
