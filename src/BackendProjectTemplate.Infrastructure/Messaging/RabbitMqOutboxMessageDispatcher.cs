using System.Reflection;
using System.Text.Json;
using BackendProjectTemplate.Domain.Common.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;

namespace BackendProjectTemplate.Infrastructure.Messaging;

public sealed class RabbitMqOutboxMessageDispatcher(
    IPublisher publisher,
    ISender sender) : IOutboxMessageDispatcher
{
    internal const string DependencyInjectionKey = "backend-project-template-rabbitmq-outbox";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly MethodInfo PublishMethod = typeof(IPublisher)
        .GetMethods()
        .Single(method => method.Name == nameof(IPublisher.PublishAsync) && method.IsGenericMethodDefinition);
    private static readonly MethodInfo SendMethod = typeof(ISender)
        .GetMethods()
        .Single(method => method.Name == nameof(ISender.SendAsync) && method.IsGenericMethodDefinition);

    public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var messageType = ResolveMessageType(message.Type);
        var payload = DeserializePayload(message.Payload, messageType);

        return message.Kind switch
        {
            OutboxMessageKind.Event => PublishEventAsync(messageType, payload, cancellationToken),
            OutboxMessageKind.Command => SendCommandAsync(messageType, payload, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported outbox message kind '{message.Kind}'.")
        };
    }

    private Task PublishEventAsync(Type messageType, object payload, CancellationToken cancellationToken)
    {
        if (!typeof(IEvent).IsAssignableFrom(messageType))
        {
            throw new InvalidOperationException($"Outbox event type '{messageType.FullName}' does not implement IEvent.");
        }

        return (Task)(PublishMethod.MakeGenericMethod(messageType)
            .Invoke(publisher, [payload, cancellationToken, null]) ?? throw new InvalidOperationException("Publisher invocation failed."));
    }

    private Task SendCommandAsync(Type messageType, object payload, CancellationToken cancellationToken)
    {
        if (!typeof(ICommand).IsAssignableFrom(messageType))
        {
            throw new InvalidOperationException($"Outbox command type '{messageType.FullName}' does not implement ICommand.");
        }

        return (Task)(SendMethod.MakeGenericMethod(messageType)
            .Invoke(sender, [payload, cancellationToken, null]) ?? throw new InvalidOperationException("Sender invocation failed."));
    }

    private static object DeserializePayload(string payload, Type messageType) =>
        JsonSerializer.Deserialize(payload, messageType, SerializerOptions)
        ?? throw new InvalidOperationException($"Outbox payload for '{messageType.FullName}' could not be deserialized.");

    private static Type ResolveMessageType(string typeName)
    {
        var resolvedType = Type.GetType(typeName, throwOnError: false)
            ?? AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, throwOnError: false, ignoreCase: false))
                .FirstOrDefault(type => type is not null);

        return resolvedType
            ?? throw new InvalidOperationException($"Unable to resolve outbox message type '{typeName}'.");
    }
}
