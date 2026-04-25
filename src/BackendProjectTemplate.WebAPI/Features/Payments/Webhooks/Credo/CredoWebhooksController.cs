using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Credo;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.Credo.Route)]
public sealed class CredoWebhooksController(ProcessCredoWebhookHandler handler) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle([FromBody] CredoWebhookRequest request, CancellationToken cancellationToken)
    {
        var command = new ProcessCredoWebhookCommand(CreateWebhook(request), JsonSerializer.Serialize(request, JsonSerializerOptions));

        await handler.HandleAsync(
            command,
            cancellationToken);

        return Ok();
    }

    private static CredoWebhook<object> CreateWebhook(CredoWebhookRequest request) =>
        new(
            request.Event,
            request.Data.Deserialize<CredoWebhookData>(JsonSerializerOptions)
            ?? throw new JsonException("Unable to deserialize Credo webhook payload."));
}
