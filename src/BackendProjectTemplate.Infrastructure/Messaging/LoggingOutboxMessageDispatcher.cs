using BackendProjectTemplate.Domain.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Infrastructure.Messaging;

public sealed class LoggingOutboxMessageDispatcher(ILogger<LoggingOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Dispatching outbox {Kind} message {MessageId} of type {Type}. Payload: {Payload}",
            message.Kind,
            message.MessageId,
            message.Type,
            message.Payload);

        return Task.CompletedTask;
    }
}
