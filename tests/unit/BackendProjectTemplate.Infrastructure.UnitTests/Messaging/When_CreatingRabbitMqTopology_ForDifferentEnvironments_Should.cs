using BackendProjectTemplate.Infrastructure.Messaging;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Messaging;

public sealed class When_CreatingRabbitMqTopology_ForDifferentEnvironments_Should
{
    private static readonly RabbitMqMessagingOptions Options = new()
    {
        ServiceName = "BackendProjectTemplate.Consumer",
        EventsExchange = "x.events.backendprojecttemplate",
        CommandsExchange = "x.commands.backendprojecttemplate"
    };

    [Fact]
    public void ProduceDistinctNamesForDifferentEnvironments()
    {
        var development = RabbitMqTopology.Create(Options, "Development");
        var container = RabbitMqTopology.Create(Options, "Container");

        development.EventsExchange.ShouldNotBe(container.EventsExchange);
        development.EventsSubscriptionName.ShouldNotBe(container.EventsSubscriptionName);
        development.CommandsQueueName.ShouldNotBe(container.CommandsQueueName);
    }
}
