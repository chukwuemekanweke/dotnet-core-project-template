using RabbitMQ.Client;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

[Collection(nameof(ContainersCollection))]
public sealed class WhenStartingConsumer_Should : ConsumerWorkerIntegrationTestBase
{
    private readonly ContainersFixture _fixture;

    public WhenStartingConsumer_Should(ContainersFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeclareServiceScopedQueues()
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _fixture.RabbitMqHostName,
            Port = _fixture.RabbitMqPort,
            UserName = _fixture.RabbitMqUserName,
            Password = _fixture.RabbitMqPassword,
            VirtualHost = _fixture.RabbitMqVirtualHost
        };

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        const string eventsQueueName = "evt.backendprojecttemplate.consumer.integrationtests.events";
        const string commandsQueueName = "cmd.backendprojecttemplate.consumer.integrationtests.commands";

        var eventsQueue = await channel.QueueDeclarePassiveAsync(eventsQueueName);
        var commandsQueue = await channel.QueueDeclarePassiveAsync(commandsQueueName);

        eventsQueue.QueueName.ShouldBe(eventsQueueName);
        commandsQueue.QueueName.ShouldBe(commandsQueueName);
    }
}
