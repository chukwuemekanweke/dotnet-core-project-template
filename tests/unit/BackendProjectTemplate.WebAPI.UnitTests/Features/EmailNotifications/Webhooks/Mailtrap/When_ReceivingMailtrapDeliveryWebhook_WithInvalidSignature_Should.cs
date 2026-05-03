using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed class When_ReceivingMailtrapDeliveryWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var context = new EmailNotificationsControllerTestContext();

        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Shared.InvalidSignature));

        const string payload = "{\"events\":[{\"event\":\"delivery\",\"message_id\":\"mailtrap-message-id\",\"sending_stream\":\"transactional\",\"email\":\"ada@example.com\",\"sending_domain_name\":\"mail.example.com\",\"timestamp\":1746273900,\"event_id\":\"evt_123\"}]}";
        var sut = new MailtrapWebhooksController(context.CreateHandler());
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        sut.ControllerContext.HttpContext.Request.Headers["Mailtrap-Signature"] = "invalid-signature";

        var result = await sut.Handle(CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedObjectResult>();
    }
}
