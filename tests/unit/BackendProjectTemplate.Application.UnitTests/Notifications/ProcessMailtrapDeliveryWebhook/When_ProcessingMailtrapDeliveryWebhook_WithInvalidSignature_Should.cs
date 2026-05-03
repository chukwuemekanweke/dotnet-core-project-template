using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Notifications.ProcessMailtrapDeliveryWebhook;

public sealed class When_ProcessingMailtrapDeliveryWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnInvalidSignature()
    {
        var context = new NotificationsFlowTestContext();

        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Shared.InvalidSignature));

        var result = await context.CreateHandler().HandleAsync(
            new ProcessMailtrapDeliveryWebhookCommand(
                [
                    new MailtrapDeliveryWebhookEvent(
                        MailtrapDeliveryWebhookEvents.Delivery,
                        "mailtrap-message-id",
                        "transactional",
                        "ada@example.com",
                        "mail.example.com",
                        1,
                        "evt_123")
                ],
                "{\"events\":[]}",
                "invalid-signature"),
            CancellationToken.None);

        result.Status.ShouldBe(MailtrapDeliveryWebhookReceiptStatus.InvalidSignature);
        await context.EmailDeliveryWebhookInboxRepository.DidNotReceive().AddAsync(Arg.Any<EmailDeliveryWebhookInbox>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
