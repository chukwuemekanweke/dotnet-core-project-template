using System.Text.Json;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenDispatchingEventOutboxMessage_ShouldPublishEventThroughRabbitMq
{
    [Fact]
    public async Task Verify()
    {
        var publisher = Substitute.For<IPublisher>();
        var sender = Substitute.For<ISender>();
        var userId = Guid.CreateVersion7();
        var emailAddress = InfrastructureTestData.Email();
        var @event = new UserCreated(userId, emailAddress);
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt);

        var sut = new RabbitMqOutboxMessageDispatcher(publisher, sender);

        await sut.DispatchAsync(outboxMessage);

        await publisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message => message.MessageId == @event.MessageId && message.UserId == userId && message.EmailAddress == emailAddress),
            Arg.Any<CancellationToken>(),
            Arg.Any<IDictionary<string, string>?>());
    }
}
