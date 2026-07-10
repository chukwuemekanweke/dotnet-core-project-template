using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Providers.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications;

internal sealed class NotificationsConsumerTestContext
{
    public NotificationsConsumerTestContext()
    {
        MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
    }

    public IReadRepository<Provider> ProviderRepository { get; } = Substitute.For<IReadRepository<Provider>>();
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IRepository<EmailNotificationLog> EmailNotificationLogRepository { get; } = Substitute.For<IRepository<EmailNotificationLog>>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IRepository<MessageInbox> MessageInboxRepository { get; } = Substitute.For<IRepository<MessageInbox>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 13, 0, 0, TimeSpan.Zero));

    public EmailDeliveryWebhookReceivedHandler CreateEmailDeliveryWebhookReceivedHandler() =>
        new(
            ProviderRepository,
            EmailDeliveryWebhookInboxRepository,
            EmailNotificationLogRepository,
            CurrentActorAccessor,
            MessageContext,
            CustomTelemetryContext,
            UnitOfWork,
            Clock,
            MessageInboxRepository);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
