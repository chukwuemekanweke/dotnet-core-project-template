using BackendProjectTemplate.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Text;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed class When_ReceivingMailtrapDeliveryWebhook_WithIncorrectEventCasing_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new EmailNotificationsControllerTestContext();
        const string payload = "{\"events\":[{\"event\":\"Bounce\",\"message_id\":\"mailtrap-message-id\",\"sending_stream\":\"transactional\",\"email\":\"ada@example.com\",\"sending_domain_name\":\"mail.example.com\",\"timestamp\":1794647100,\"event_id\":\"evt_123\",\"response\":\"Mailbox unavailable\"}]}";
        var sut = new MailtrapWebhooksController(context.CreateHandler(), NullLogger<MailtrapWebhooksController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var result = await sut.Handle(CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
