using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithAlreadyProcessedWebhook_Should
{
    [Fact]
    public async Task ReturnWithoutUpdatingNotificationLog()
    {
        var context = new NotificationsConsumerTestContext();
        var providerId = Guid.CreateVersion7();
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            "evt_123",
            "mailtrap-message-id",
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            DateTimeOffset.Parse("2026-05-03T13:01:00+00:00"),
            context.Clock.GetUtcNow());

        inbox.MarkProcessed(KnownWebhookStatusChangeReasons.Notifications.NotificationLogDelivered, context.Clock.GetUtcNow());
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(inbox);

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_123"
            },
            CancellationToken.None);

        await context.EmailNotificationLogRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(), Arg.Any<CancellationToken>());
        context.EmailNotificationLogRepository.DidNotReceive().Update(Arg.Any<EmailNotificationLog>());
        context.EmailDeliveryWebhookInboxRepository.DidNotReceive().Update(Arg.Any<EmailDeliveryWebhookInbox>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
    }
}
