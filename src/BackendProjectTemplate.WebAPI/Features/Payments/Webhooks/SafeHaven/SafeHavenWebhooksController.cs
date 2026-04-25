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
        var command = new ProcessSafeHavenWebhookCommand(CreateWebhook(request), JsonSerializer.Serialize(request, JsonSerializerOptions));

        await handler.HandleAsync(
            command,
            cancellationToken);

        return Ok();
    }

    private static SafeHavenWebhook<object> CreateWebhook(SafeHavenWebhookRequest request) =>
        request.Event switch
        {
            SafeHavenWebhookEvents.AccountCredit => new SafeHavenWebhook<object>(
                request.Event,
                request.Data.Deserialize<SafeHavenAccountCreditWebhookData>(JsonSerializerOptions)
                ?? throw new JsonException("Unable to deserialize SafeHaven account credit webhook payload.")),
            SafeHavenWebhookEvents.AccountDebit => new SafeHavenWebhook<object>(
                request.Event,
                request.Data.Deserialize<SafeHavenAccountDebitWebhookData>(JsonSerializerOptions)
                ?? throw new JsonException("Unable to deserialize SafeHaven account debit webhook payload.")),
            SafeHavenWebhookEvents.VirtualAccountTransfer => new SafeHavenWebhook<object>(
                request.Event,
                request.Data.Deserialize<SafeHavenVirtualAccountTransferWebhookData>(JsonSerializerOptions)
                ?? throw new JsonException("Unable to deserialize SafeHaven virtual account transfer webhook payload.")),
            _ => new SafeHavenWebhook<object>(request.Event, request.Data)
        };
}
