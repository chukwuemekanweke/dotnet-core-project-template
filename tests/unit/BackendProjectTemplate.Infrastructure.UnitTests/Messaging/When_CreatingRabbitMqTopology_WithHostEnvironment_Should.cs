using BackendProjectTemplate.Infrastructure.Messaging;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Messaging;

public sealed class When_CreatingRabbitMqTopology_WithHostEnvironment_Should
{
    private static readonly RabbitMqMessagingOptions Options = new()
    {
        ServiceName = "BackendProjectTemplate.Consumer",
        EventsExchange = "x.events.backendprojecttemplate",
        CommandsExchange = "x.commands.backendprojecttemplate"
    };

    [Fact]
    public void ScopeExchangesAndQueuesByEnvironment()
    {
        var topology = RabbitMqTopology.Create(Options, "Production");

        topology.Environment.ShouldBe("production");
        topology.EventsExchange.ShouldBe("x.events.backendprojecttemplate.production");
        topology.CommandsExchange.ShouldBe("x.commands.backendprojecttemplate.production");
        topology.EventsSubscriptionName.ShouldBe("BackendProjectTemplate.Consumer.production.events");
        topology.CommandsQueueName.ShouldBe("BackendProjectTemplate.Consumer.production.commands");
    }
}
