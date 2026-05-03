using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Notifications.ProcessMailtrapDeliveryWebhook;

public sealed class When_ProcessingMailtrapDeliveryWebhook_WithValidSignature_Should
{
    [Fact]
    public async Task PersistWebhookAndMarkLogDelivered()
    {
        var context = new NotificationsFlowTestContext();
        var provider = context.CreateMailtrapProvider();
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
        log.MarkSent("mailtrap-message-id", context.Clock.GetUtcNow());
        EmailDeliveryWebhookInbox? capturedInbox = null;

        context.ProviderRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(true, "signature_verified"));
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((EmailDeliveryWebhookInbox?)null);
        context.EmailDeliveryWebhookInboxRepository.AddAsync(Arg.Do<EmailDeliveryWebhookInbox>(inbox => capturedInbox = inbox), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.EmailNotificationLogRepository.FirstOrDefaultAsync(Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var result = await context.CreateHandler().HandleAsync(
            new ProcessMailtrapDeliveryWebhookCommand(
                [
                    new MailtrapDeliveryWebhookEvent(
                        MailtrapDeliveryWebhookEvents.Delivery,
                        "mailtrap-message-id",
                        "transactional",
                        "ada@example.com",
                        "mail.example.com",
                        DateTimeOffset.Parse("2026-05-03T12:05:00+00:00").ToUnixTimeSeconds(),
                        "evt_123")
                ],
                "{\"events\":[]}",
                "valid-signature"),
            CancellationToken.None);

        result.Status.ShouldBe(MailtrapDeliveryWebhookReceiptStatus.Persisted);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.ProviderMessageId.ShouldBe("mailtrap-message-id");
        log.DeliveredAtUtc.ShouldBe(DateTimeOffset.Parse("2026-05-03T12:05:00+00:00"));
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
