using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SafeHavenAccountCreditCommandData = BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenAccountCreditWebhookData;
using SafeHavenAccountDebitCommandData = BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenAccountDebitWebhookData;
using SafeHavenVirtualAccountTransferCommandData = BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenVirtualAccountTransferWebhookData;

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
        var rawPayload = JsonSerializer.Serialize(request, JsonSerializerOptions);

        return request.Event switch
        {
            SafeHavenWebhookEvents.AccountCredit => await HandleAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenAccountCreditWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven account credit webhook payload.")),
                rawPayload,
                cancellationToken),
            SafeHavenWebhookEvents.AccountDebit => await HandleAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenAccountDebitWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven account debit webhook payload.")),
                rawPayload,
                cancellationToken),
            SafeHavenWebhookEvents.VirtualAccountTransfer => await HandleAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenVirtualAccountTransferWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven virtual account transfer webhook payload.")),
                rawPayload,
                cancellationToken),
            _ => await HandleAsync(request.Event, request.Data, rawPayload, cancellationToken)
        };
    }

    private async Task<IActionResult> HandleAsync<TData>(
        string eventName,
        TData data,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var command = new ProcessSafeHavenWebhookCommand<TData>(new SafeHavenWebhook<TData>(eventName, data), rawPayload);

        await handler.HandleAsync(command, cancellationToken);

        return Ok();
    }

    private static SafeHavenAccountCreditCommandData Map(SafeHavenAccountCreditWebhookData data) =>
        new(
            data.Queued,
            data.LimitExceeded,
            data.Id,
            data.Client,
            data.Account,
            data.TransactionType,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.MandateReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.RealDebitAccountName,
            data.RealDebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.UpdatedAt);

    private static SafeHavenAccountDebitCommandData Map(SafeHavenAccountDebitWebhookData data) =>
        new(
            data.Queued,
            data.LimitExceeded,
            data.Id,
            data.Client,
            data.Account,
            data.TransactionType,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.MandateReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.CreatedBy,
            data.UpdatedAt);

    private static SafeHavenVirtualAccountTransferCommandData Map(SafeHavenVirtualAccountTransferWebhookData data) =>
        new(
            data.Id,
            data.Client,
            data.VirtualAccount,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.DeclinedAt,
            data.UpdatedAt);
}
