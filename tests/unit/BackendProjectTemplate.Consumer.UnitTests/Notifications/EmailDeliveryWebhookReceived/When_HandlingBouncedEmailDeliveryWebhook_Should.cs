using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BackendProjectTemplate.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingBouncedEmailDeliveryWebhook_Should
{
    [Fact]
    public async Task MarkNotificationLogBounced()
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
        var bouncedAtUtc = DateTimeOffset.Parse("2026-11-14T09:05:00+00:00");
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            "evt_bounce",
            "bounce",
            "mailtrap-message-id",
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            bouncedAtUtc,
            context.Clock.GetUtcNow(),
            "Mailbox unavailable");
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
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_bounce"
            },
            CancellationToken.None);

        log.DeliveryStatus.ShouldBe(EmailDeliveryStatus.Bounced);
        log.FailedAtUtc.ShouldBe(bouncedAtUtc);
        log.DeliveryFailureReason.ShouldBe("Mailbox unavailable");
        context.CustomTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Notifications.EmailBounced,
            Arg.Any<Dictionary<string, string>>());
    }
}
