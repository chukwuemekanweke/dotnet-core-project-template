using BackendProjectTemplate.Consumer;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class When_HandlingMessage_WithExistingInboxEntry_Should
{
    [Fact]
    public async Task SkipMessage()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 7, 10, 22, 0, 0, TimeSpan.Zero));
        var tenantId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var messageType = typeof(TestEvent).FullName!;
        var inbox = MessageInbox.Create(messageId, messageType, timeProvider.GetUtcNow());
        var handler = new TestEventHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            messageInboxRepository,
            unitOfWork,
            timeProvider);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        messageInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<MessageInbox>>(), Arg.Any<CancellationToken>())
            .Returns(inbox);

        await handler.HandleAsync(new TestEvent { MessageId = messageId, TenantId = tenantId }, CancellationToken.None);

        handler.HandledCount.ShouldBe(0);
        await messageInboxRepository.DidNotReceive().AddAsync(Arg.Any<MessageInbox>(), Arg.Any<CancellationToken>());
        messageInboxRepository.DidNotReceive().Update(Arg.Any<MessageInbox>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed record TestEvent : BaseEvent;

    private sealed class TestEventHandler(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext,
        IRepository<MessageInbox> messageInboxRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : BaseMessageHandler<TestEvent>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider)
    {
        public int HandledCount { get; private set; }

        protected override Task HandleAsyncInternal(TestEvent message, CancellationToken cancellationToken)
        {
            HandledCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
