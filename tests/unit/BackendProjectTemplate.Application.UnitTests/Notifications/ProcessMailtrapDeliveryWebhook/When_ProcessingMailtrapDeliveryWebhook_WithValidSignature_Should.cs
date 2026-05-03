using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Notifications.ProcessMailtrapDeliveryWebhook;

public sealed class When_ProcessingMailtrapDeliveryWebhook_WithValidSignature_Should
{
    [Fact]
    public async Task PersistWebhookAndPublishDeliveryEvent()
    {
        var context = new NotificationsFlowTestContext();
        var provider = context.CreateMailtrapProvider();
        EmailDeliveryWebhookInbox? capturedInbox = null;

        context.ProviderRepository.FirstOrDefaultAsync(Arg.Any<ProviderByTypeAndKeySpecification>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(true, "signature_verified"));
        context.EmailDeliveryWebhookInboxRepository.ListAsync(Arg.Any<EmailDeliveryWebhookInboxesByEventIdsSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);
        context.EmailDeliveryWebhookInboxRepository.AddAsync(Arg.Do<EmailDeliveryWebhookInbox>(inbox => capturedInbox = inbox), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

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
        capturedInbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<EmailDeliveryWebhookReceived>(message =>
                message.ProviderId == provider.Id &&
                message.EventId == "evt_123" &&
                message.ProviderMessageId == "mailtrap-message-id"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
