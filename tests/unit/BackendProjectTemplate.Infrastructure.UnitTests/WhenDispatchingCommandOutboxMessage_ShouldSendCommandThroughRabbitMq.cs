using System.Text.Json;
using BackendProjectTemplate.Contracts.Commands;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenDispatchingCommandOutboxMessage_ShouldSendCommandThroughRabbitMq
{
    [Fact]
    public async Task Verify()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IPublisher>();
        var command = new TestCommand("welcome-email");
        var outboxMessage = OutboxMessage.CreateCommand(
            command.MessageId,
            typeof(TestCommand).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(command),
            command.RequestedAt);

        var sut = new RabbitMqOutboxMessageDispatcher(publisher, sender);

        await sut.DispatchAsync(outboxMessage);

        await sender.Received(1).SendAsync(
            Arg.Is<TestCommand>(message => message.MessageId == command.MessageId && message.Name == command.Name),
            Arg.Any<CancellationToken>(),
            Arg.Any<IDictionary<string, string>?>());
    }

    private sealed record TestCommand(string Name) : BaseCommand;
}
