using Asp.Versioning;
using BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.EmailNotificationWebhooks.Mailtrap.Route)]
public sealed class MailtrapWebhooksController(
    ProcessMailtrapDeliveryWebhookHandler handler,
    ILogger<MailtrapWebhooksController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var rawPayloadBytes = await ReadRawPayloadAsync(cancellationToken);
        var rawPayload = Encoding.UTF8.GetString(rawPayloadBytes);
        var request = JsonSerializer.Deserialize<MailtrapDeliveryWebhookRequest>(rawPayload, JsonSerializerOptions)
            ?? throw new JsonException("Unable to deserialize Mailtrap delivery webhook payload.");

        if (request.Events.Length == 0)
        {
            return BadRequest("At least one delivery event is required.");
        }

        if (request.Events.Any(item => !MailtrapDeliveryWebhookEvents.IsSupported(item.Event)))
        {
            return BadRequest("Unsupported Mailtrap webhook event.");
        }

        var result = await handler.HandleAsync(
            new ProcessMailtrapDeliveryWebhookCommand(
                request.Events
                    .Select(item => new MailtrapDeliveryWebhookEvent(
                        item.Event,
                        item.MessageId,
                        item.SendingStream,
                        item.Email,
                        item.SendingDomainName,
                        item.Timestamp,
                        item.EventId,
                        item.Reason,
                        item.Response,
                        item.ResponseCode,
                        item.BounceCategory))
                    .ToArray(),
                rawPayload,
                rawPayloadBytes,
                Request.Headers["Mailtrap-Signature"]),
            cancellationToken);

        if (result.Status == MailtrapDeliveryWebhookReceiptStatus.InvalidSignature)
        {
            logger.LogWarning(
                "Mailtrap webhook signature validation failed. Reason: {Reason}",
                result.StatusChangeReason ?? "unknown");
            return Unauthorized("Invalid signature");
        }

        return Ok();
    }

    private async Task<byte[]> ReadRawPayloadAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        Request.Body.Position = 0;

        return buffer.ToArray();
    }
}
