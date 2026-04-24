using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.SafeHaven;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.SafeHaven.Route)]
public sealed class SafeHavenWebhooksController(ProcessSafeHavenWebhookHandler handler) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle([FromBody] SafeHavenWebhookRequest request, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ProcessSafeHavenWebhookCommand(JsonSerializer.Serialize(request, JsonSerializerOptions)),
            cancellationToken);

        return Ok();
    }
}
