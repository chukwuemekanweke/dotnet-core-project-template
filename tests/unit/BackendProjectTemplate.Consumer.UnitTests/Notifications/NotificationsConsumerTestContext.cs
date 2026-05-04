using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Providers.Entities;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications;

internal sealed class NotificationsConsumerTestContext
{
    public IReadRepository<Provider> ProviderRepository { get; } = Substitute.For<IReadRepository<Provider>>();
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IRepository<EmailNotificationLog> EmailNotificationLogRepository { get; } = Substitute.For<IRepository<EmailNotificationLog>>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 13, 0, 0, TimeSpan.Zero));

    public EmailDeliveryWebhookReceivedHandler CreateEmailDeliveryWebhookReceivedHandler() =>
        new(ProviderRepository, EmailDeliveryWebhookInboxRepository, EmailNotificationLogRepository, CurrentActor, CustomTelemetryContext, UnitOfWork, Clock);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
