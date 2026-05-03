using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Notifications.Entities;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications;

internal sealed class NotificationsConsumerTestContext
{
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IRepository<EmailNotificationLog> EmailNotificationLogRepository { get; } = Substitute.For<IRepository<EmailNotificationLog>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 13, 0, 0, TimeSpan.Zero));

    public EmailDeliveryWebhookReceivedHandler CreateEmailDeliveryWebhookReceivedHandler() =>
        new(EmailDeliveryWebhookInboxRepository, EmailNotificationLogRepository, UnitOfWork, Clock);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
