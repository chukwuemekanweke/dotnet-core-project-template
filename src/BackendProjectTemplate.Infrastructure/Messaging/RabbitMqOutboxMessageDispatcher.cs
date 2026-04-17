using System.Reflection;
using System.Text.Json;
using BackendProjectTemplate.Domain.Common.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Messaging;

public sealed class RabbitMqOutboxMessageDispatcher(
    [FromKeyedServices(RabbitMqOutboxMessageDispatcherConstants.DependencyInjectionKey)] IPublisher publisher,
    [FromKeyedServices(RabbitMqOutboxMessageDispatcherConstants.DependencyInjectionKey)] ISender sender) : IOutboxMessageDispatcher
{
    private const string ParentOperationIdHeader = "cimr-parent-operation-id";
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
            OutboxMessageKind.Event => PublishEventAsync(message, messageType, payload, cancellationToken),
            OutboxMessageKind.Command => SendCommandAsync(message, messageType, payload, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported outbox message kind '{message.Kind}'.")
        };
    }

    private Task PublishEventAsync(OutboxMessage message, Type messageType, object payload, CancellationToken cancellationToken)
    {
        if (!typeof(IEvent).IsAssignableFrom(messageType))
        {
            throw new InvalidOperationException($"Outbox event type '{messageType.FullName}' does not implement IEvent.");
        }

        var extraHeaders = BuildExtraHeaders(message);
        return (Task)(PublishMethod.MakeGenericMethod(messageType)
            .Invoke(publisher, [payload, cancellationToken, extraHeaders]) ?? throw new InvalidOperationException("Publisher invocation failed."));
    }

    private Task SendCommandAsync(OutboxMessage message, Type messageType, object payload, CancellationToken cancellationToken)
    {
        if (!typeof(ICommand).IsAssignableFrom(messageType))
        {
            throw new InvalidOperationException($"Outbox command type '{messageType.FullName}' does not implement ICommand.");
        }

        var extraHeaders = BuildExtraHeaders(message);
        return (Task)(SendMethod.MakeGenericMethod(messageType)
            .Invoke(sender, [payload, cancellationToken, extraHeaders]) ?? throw new InvalidOperationException("Sender invocation failed."));
    }

    private static IDictionary<string, string>? BuildExtraHeaders(OutboxMessage message) =>
        CreateHeaders(message);

    private static IDictionary<string, string>? CreateHeaders(OutboxMessage message)
    {
        Dictionary<string, string>? headers = null;

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KnownMetadata.CorrelationId] = message.CorrelationId
            };
        }

        if (!string.IsNullOrWhiteSpace(message.ActivityId))
        {
            headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            headers[ParentOperationIdHeader] = message.ActivityId;
        }

        return headers;
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
