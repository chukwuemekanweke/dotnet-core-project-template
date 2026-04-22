using BackendProjectTemplate.Consumer;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class When_HandlingBaseEvent_WithoutFlowId_Should
{
    [Fact]
    public async Task SetEmptyFlowId()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var tenantId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7().ToString("N");
        messageContext.CorrelationId.Returns(correlationId);

        await new UserCreatedHandlerForTests(customTelemetryContext, currentActorAccessor, messageContext)
            .HandleAsync(
                new UserCreated
                {
                    TenantId = tenantId,
                    FlowId = null
                },
                CancellationToken.None);

        currentActorAccessor.Received(1).Set(
            Arg.Any<string>(),
            tenantId,
            correlationId,
            string.Empty);
    }

    private sealed class UserCreatedHandlerForTests(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor, messageContext)
    {
        protected override Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
