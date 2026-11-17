using Asp.Versioning;
using BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Kyc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendProjectTemplate.WebAPI.Features.Kyc.Webhooks;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.KycWebhooks.Kycaid.Route)]
public sealed class KycaidWebhooksController(
    IKycCallbackSignatureValidator signatureValidator,
    ReceiveKycCallbackHandler handler,
    ICustomTelemetryContext telemetryContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var rawPayload = buffer.ToArray();
        if (!signatureValidator.IsValid(rawPayload, Request.Headers["x-data-integrity"]))
        {
            telemetryContext.AddCustomEvent("kyc_webhook_invalid_signature");
            return Unauthorized("Invalid signature.");
        }

        KycaidCallback? callback;
        try
        {
            callback = JsonSerializer.Deserialize<KycaidCallback>(rawPayload, JsonOptions);
        }
        catch (JsonException)
        {
            telemetryContext.AddCustomEvent("kyc_webhook_malformed_payload");
            return BadRequest("Invalid callback payload.");
        }

        var applicantReference = callback?.ApplicantId ?? callback?.Applicant?.ApplicantId;
        if (callback is null ||
            string.IsNullOrWhiteSpace(callback.RequestId) ||
            string.IsNullOrWhiteSpace(callback.Type) ||
            string.IsNullOrWhiteSpace(applicantReference) ||
            (string.IsNullOrWhiteSpace(callback.VerificationId) && string.IsNullOrWhiteSpace(callback.FormToken)))
        {
            telemetryContext.AddCustomEvent("kyc_webhook_malformed_payload");
            return BadRequest("Callback request_id, type, applicant_id and a verification or form reference are required.");
        }

        var result = await handler.HandleAsync(new(
            callback.RequestId,
            callback.Type,
            callback.VerificationId,
            callback.FormToken,
            applicantReference,
            callback.ExternalApplicantId ?? callback.Applicant?.ExternalApplicantId,
            callback.VerificationStatus ?? callback.Status ?? "unknown",
            callback.Verified), cancellationToken);
        telemetryContext.AddCustomEvent(
            result == ReceiveKycCallbackStatus.Persisted ? "kyc_webhook_accepted" : "kyc_webhook_duplicate");
        return Ok();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record KycaidCallback(
        string Type,
        [property: JsonPropertyName("request_id")] string RequestId,
        [property: JsonPropertyName("verification_id")] string? VerificationId,
        [property: JsonPropertyName("form_token")] string? FormToken,
        [property: JsonPropertyName("applicant_id")] string? ApplicantId,
        [property: JsonPropertyName("external_applicant_id")] string? ExternalApplicantId,
        [property: JsonPropertyName("verification_status")] string? VerificationStatus,
        string? Status,
        bool? Verified,
        KycaidApplicant? Applicant);

    private sealed record KycaidApplicant(
        [property: JsonPropertyName("applicant_id")] string? ApplicantId,
        [property: JsonPropertyName("external_applicant_id")] string? ExternalApplicantId);
}
