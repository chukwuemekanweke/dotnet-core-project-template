using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;
using NSubstitute;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithMatchingNotificationLog_Should
{
    [Fact]
    public async Task MarkNotificationLogDelivered()
    {
        var context = new NotificationsConsumerTestContext();
        var providerId = Guid.CreateVersion7();
        var log = EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            context.Clock.GetUtcNow());
        var deliveredAtUtc = DateTimeOffset.Parse("2026-05-03T13:05:00+00:00");
        var provider = Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true, context.Clock.GetUtcNow());
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            "evt_123",
            "mailtrap-message-id",
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            deliveredAtUtc,
            context.Clock.GetUtcNow());

        log.MarkSent("mailtrap-message-id", context.Clock.GetUtcNow());
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(inbox);
        context.EmailNotificationLogRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(log);
        context.ProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ProviderByIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_123"
            },
            CancellationToken.None);

        log.DeliveredAtUtc.ShouldBe(deliveredAtUtc);
        inbox.WebhookProcessingStatus.ShouldBe(BackendProjectTemplate.Contracts.Payments.WebhookProcessingStatus.Processed);
        context.EmailNotificationLogRepository.Received(1).Update(log);
        context.EmailDeliveryWebhookInboxRepository.Received(1).Update(inbox);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        context.CustomTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Notifications.EmailDelivered,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.MessageIdPropertyName] == log.MessageId.ToString() &&
                properties[Observability.ProviderKeyPropertyName] == "mailtrap" &&
                properties[Observability.ProviderMessageIdPropertyName] == "mailtrap-message-id" &&
                properties[Observability.NotificationTypePropertyName] == NotificationType.SignInSuccessful.ToString()));
    }
}
