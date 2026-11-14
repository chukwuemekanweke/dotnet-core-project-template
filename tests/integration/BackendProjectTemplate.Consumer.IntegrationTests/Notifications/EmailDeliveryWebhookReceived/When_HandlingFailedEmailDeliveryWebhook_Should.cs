using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Notifications.EmailDeliveryWebhookReceived;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingFailedEmailDeliveryWebhook_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private readonly ContainersFixture _fixture = fixture;
    private Guid _messageId;
    private Guid _webhookInboxId;

    protected override Task InitializeWorkerTestAsync() => Task.CompletedTask;

    protected override Task DisposeWorkerTestAsync() => DeleteSeedDataAsync();

    [Theory]
    [InlineData("bounce", EmailDeliveryStatus.Bounced, "Mailbox unavailable")]
    [InlineData("reject", EmailDeliveryStatus.Rejected, "Recipient is suppressed")]
    public async Task MarkNotificationLogFailed(
        string eventType,
        EmailDeliveryStatus expectedStatus,
        string failureReason)
    {
        var seed = await SeedNotificationLogAsync(eventType, failureReason);

        await PublishEmailDeliveryWebhookReceivedAsync(seed.ProviderId, seed.ProviderMessageId, seed.EventId);

        await WaitForConditionAsync(async () =>
        {
            using var scope = CreateDbContextScope();
            var log = await scope.DbContext.EmailNotificationLogs
                .FirstOrDefaultAsync(item => item.MessageId == _messageId);
            var inbox = await scope.DbContext.EmailDeliveryWebhookInboxes
                .FirstOrDefaultAsync(item => item.Id == _webhookInboxId);

            return log?.DeliveryStatus == expectedStatus &&
                log.FailedAtUtc == seed.FailedAtUtc &&
                inbox?.WebhookProcessingStatus == WebhookProcessingStatus.Processed;
        });

        using var assertionScope = CreateDbContextScope();
        var persistedLog = await assertionScope.DbContext.EmailNotificationLogs
            .FirstAsync(item => item.MessageId == _messageId);
        var persistedInbox = await assertionScope.DbContext.EmailDeliveryWebhookInboxes
            .FirstAsync(item => item.Id == _webhookInboxId);

        persistedLog.DeliveryStatus.ShouldBe(expectedStatus);
        persistedLog.FailedAtUtc.ShouldBe(seed.FailedAtUtc);
        persistedLog.LastDeliveryEventAtUtc.ShouldBe(seed.FailedAtUtc);
        persistedLog.DeliveryFailureReason.ShouldBe(failureReason);
        persistedLog.FailureReason.ShouldBeNull();
        persistedInbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
    }

    private async Task<SeedResult> SeedNotificationLogAsync(string eventType, string failureReason)
    {
        _messageId = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var providerMessageId = $"mailtrap-{Guid.CreateVersion7():N}";
        var eventId = $"evt-{Guid.CreateVersion7():N}";
        var failedAtUtc = DateTimeOffset.Parse("2026-11-14T14:05:00+00:00");

        using var scope = CreateScope();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = EmailNotificationLog.Create(
            _messageId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            timeProvider.GetUtcNow());
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            eventId,
            eventType,
            providerMessageId,
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            failedAtUtc,
            timeProvider.GetUtcNow(),
            failureReason);

        log.MarkSent(providerMessageId, timeProvider.GetUtcNow());
        await dbContext.EmailDeliveryWebhookInboxes.AddAsync(inbox);
        await dbContext.EmailNotificationLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
        _webhookInboxId = inbox.Id;

        return new SeedResult(providerId, providerMessageId, eventId, failedAtUtc);
    }

    private async Task PublishEmailDeliveryWebhookReceivedAsync(
        Guid providerId,
        string providerMessageId,
        string eventId)
    {
        var publisherConfig = new PublisherConfig
        {
            ServiceName = "BackendProjectTemplate.Consumer.IntegrationTests.Publisher",
            HostName = _fixture.RabbitMqHostName,
            Port = _fixture.RabbitMqPort,
            UserName = _fixture.RabbitMqUserName,
            Password = _fixture.RabbitMqPassword,
            VirtualHost = _fixture.RabbitMqVirtualHost,
            EventsExchange = "x.events.backendprojecttemplate.integrationtests"
        };

        await using var publisherServices = new ServiceCollection()
            .AddLogging()
            .AddPublisher(publisherConfig)
            .BuildServiceProvider();
        var publisher = publisherServices.GetRequiredKeyedService<IPublisher>(publisherConfig.Key);
        await publisher.PublishAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = providerMessageId,
                EventId = eventId
            },
            CancellationToken.None,
            new Dictionary<string, string>
            {
                [KnownMetadata.CorrelationId] = Guid.CreateVersion7().ToString("N")
            });
    }

    private async Task DeleteSeedDataAsync()
    {
        if (_messageId == Guid.Empty)
        {
            return;
        }

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inbox = await dbContext.EmailDeliveryWebhookInboxes
            .FirstOrDefaultAsync(item => item.Id == _webhookInboxId);
        var log = await dbContext.EmailNotificationLogs
            .FirstOrDefaultAsync(item => item.MessageId == _messageId);

        if (inbox is not null)
        {
            dbContext.EmailDeliveryWebhookInboxes.Remove(inbox);
        }

        if (log is not null)
        {
            dbContext.EmailNotificationLogs.Remove(log);
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed record SeedResult(
        Guid ProviderId,
        string ProviderMessageId,
        string EventId,
        DateTimeOffset FailedAtUtc);
}
