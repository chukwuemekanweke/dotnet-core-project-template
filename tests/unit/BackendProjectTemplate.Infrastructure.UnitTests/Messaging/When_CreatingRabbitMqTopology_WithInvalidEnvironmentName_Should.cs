using BackendProjectTemplate.Infrastructure.Messaging;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Messaging;

public sealed class When_CreatingRabbitMqTopology_WithInvalidEnvironmentName_Should
{
    private static readonly RabbitMqMessagingOptions Options = new()
    {
        ServiceName = "BackendProjectTemplate.Consumer",
        EventsExchange = "x.events.backendprojecttemplate",
        CommandsExchange = "x.commands.backendprojecttemplate"
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Local Dev")]
    [InlineData("prod.eu")]
    public void RejectEnvironmentNamesThatCannotScopeNames(string hostEnvironmentName)
    {
        Should.Throw<InvalidOperationException>(() => RabbitMqTopology.Create(Options, hostEnvironmentName));
    }
}
