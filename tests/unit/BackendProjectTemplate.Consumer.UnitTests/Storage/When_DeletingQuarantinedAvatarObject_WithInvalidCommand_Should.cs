using BackendProjectTemplate.Consumer.Storage;
using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Storage;

public sealed class When_DeletingQuarantinedAvatarObject_WithInvalidCommand_Should
{
    [Fact]
    public async Task RejectWithoutDeletingObject()
    {
        var messageContext = Substitute.For<IMessageContext>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var sut = new DeleteQuarantinedAvatarObjectHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            objectStorageService,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<ILogger<DeleteQuarantinedAvatarObjectHandler>>());
        var command = new DeleteQuarantinedAvatarObject(Guid.Empty, string.Empty)
        {
            TenantId = Guid.CreateVersion7()
        };

        var action = () => sut.HandleAsync(command, CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
        await objectStorageService.DidNotReceiveWithAnyArgs().DeletePrivateObjectAsync(default!, default);
    }
}
