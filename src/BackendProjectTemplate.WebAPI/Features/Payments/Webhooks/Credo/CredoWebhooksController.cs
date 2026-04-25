using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CredoCommandCustomer = BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook.CredoWebhookCustomer;
using CredoCommandData = BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook.CredoWebhookData;

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
        var command = new ProcessCredoWebhookCommand(
            CreateWebhook(request),
            JsonSerializer.Serialize(request, JsonSerializerOptions));

        await handler.HandleAsync(
            command,
            cancellationToken);

        return Ok();
    }

    private static CredoWebhook CreateWebhook(CredoWebhookRequest request) =>
        new(
            request.Event,
            Map(
                request.Data.Deserialize<CredoWebhookData>(JsonSerializerOptions)
                ?? throw new JsonException("Unable to deserialize Credo webhook payload.")));

    private static CredoCommandData Map(CredoWebhookData data) =>
        new(
            data.BusinessCode,
            data.TransRef,
            data.BusinessRef,
            data.DebitedAmount,
            data.TransAmount,
            data.TransFeeAmount,
            data.SettlementAmount,
            data.CustomerId,
            data.TransactionDate,
            data.ChannelId,
            data.CurrencyCode,
            data.Status,
            data.PaymentMethodType,
            data.PaymentMethod,
            new CredoCommandCustomer(
                data.Customer.CustomerEmail,
                data.Customer.FirstName,
                data.Customer.LastName,
                data.Customer.PhoneNumber));
}
