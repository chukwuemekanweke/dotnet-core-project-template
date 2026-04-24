using System.Text.Json;
using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Credo;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.Credo.Route)]
public sealed class CredoWebhooksController(ProcessCredoWebhookHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ProcessCredoWebhookCommand(payload.GetRawText()),
            cancellationToken);

        return Ok();
    }
}
