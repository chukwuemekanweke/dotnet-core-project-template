using System.Text.Json;
using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessStripeWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Stripe;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.Stripe.Route)]
public sealed class StripeWebhooksController(ProcessStripeWebhookHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ProcessStripeWebhookCommand(payload.GetRawText()),
            cancellationToken);

        return Ok();
    }
}
