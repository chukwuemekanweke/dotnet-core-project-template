using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Bachs;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.Bachs.Route)]
public sealed class BachsWebhooksController(ProcessBachsWebhookHandler handler) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var rawPayloadBytes = buffer.ToArray();
        var rawPayload = Encoding.UTF8.GetString(rawPayloadBytes);
        BachsWebhookRequest request;
        try
        {
            request = JsonSerializer.Deserialize<BachsWebhookRequest>(rawPayload, JsonSerializerOptions)
                ?? throw new JsonException("Bachs webhook payload was empty.");
        }
        catch (JsonException)
        {
            return BadRequest("Invalid webhook payload");
        }

        var result = await handler.HandleAsync(
            new ProcessBachsWebhookCommand(
                Map(request),
                rawPayload,
                rawPayloadBytes,
                Request.Headers["X-Bachs-Signature-V2"],
                Request.Headers["X-Bachs-Signature"],
                Request.Headers["X-Bachs-Timestamp"]),
            cancellationToken);

        return result.Status == WebhookReceiptStatus.InvalidSignature
            ? BadRequest("Invalid signature")
            : Ok();
    }

    private static BachsWebhook Map(BachsWebhookRequest request) =>
        new(
            request.Id,
            request.Type,
            request.CreatedAt,
            request.OrganizationId,
            new BachsWebhookData(
                request.Data.ChargeId,
                request.Data.CheckoutId,
                request.Data.Reference,
                request.Data.Status,
                request.Data.Amount,
                request.Data.Currency));
}
