using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.Domain.Providers.Entities;
using NSubstitute;

namespace BackendProjectTemplate.Application.UnitTests.Notifications;

internal sealed class NotificationsFlowTestContext
{
    public IReadRepository<Provider> ProviderRepository { get; } = Substitute.For<IReadRepository<Provider>>();
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IMailtrapWebhookSignatureValidator MailtrapWebhookSignatureValidator { get; } = Substitute.For<IMailtrapWebhookSignatureValidator>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

    public ProcessMailtrapDeliveryWebhookHandler CreateHandler() =>
        new(
            ProviderRepository,
            EmailDeliveryWebhookInboxRepository,
            MailtrapWebhookSignatureValidator,
            EventPublisher,
            UnitOfWork,
            Clock);

    public Provider CreateMailtrapProvider() =>
        Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true, Clock.GetUtcNow());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
