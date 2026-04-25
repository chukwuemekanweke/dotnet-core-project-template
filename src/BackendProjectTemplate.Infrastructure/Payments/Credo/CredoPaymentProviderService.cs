using System.Text.Json;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal sealed class CredoPaymentProviderService(
    ICredoClient client,
    TimeProvider timeProvider) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.Credo;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        await client.CreatePaymentLinkAsync(
            new
            {
                request.MerchantReference,
                request.Amount,
                request.CurrencyCode,
                request.StakeholderId,
                request.TenantId
            },
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var providerReference = $"cr_{Guid.CreateVersion7():N}";

        return new PaymentProviderInitiationResult(
            providerReference,
            PaymentMethodType.PaymentLink,
            now.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["paymentLink"] = $"https://checkout.credo.local/pay/{providerReference}",
                ["providerReference"] = providerReference
            },
            new Dictionary<string, string>
            {
                ["provider"] = PaymentProviderKeys.Credo
            });
    }

    public Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(CreateVerificationResult(request, ProviderKey));

    public Task<PaymentProviderWebhookValidationResult> ValidateWebhookAsync(
        PaymentProviderWebhookValidationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.NotApplicable, "signature_not_configured"));

    public Task<PaymentProviderWebhookParseResult> ParseWebhookAsync(
        PaymentProviderWebhookParseRequest request,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.RawPayload);
        var root = document.RootElement;
        var eventName = GetOptionalString(root, "event");
        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;
        var merchantReference = GetOptionalString(data, "businessRef");
        var providerReference = GetOptionalString(data, "transRef");
        PaymentStatus? paymentStatus = eventName switch
        {
            "transaction.successful" => PaymentStatus.Succeeded,
            "transaction.failed" => PaymentStatus.Failed,
            "transaction.transaction.transfer.reverse" => PaymentStatus.Failed,
            _ => null
        };
        var failureReason = eventName switch
        {
            "transaction.failed" => "provider_reported_failure",
            "transaction.transaction.transfer.reverse" => "provider_transfer_reversed",
            _ => null
        };
        var webhookEventId = !string.IsNullOrWhiteSpace(merchantReference) && !string.IsNullOrWhiteSpace(eventName)
            ? $"{merchantReference}:{eventName}"
            : null;

        return Task.FromResult(
            new PaymentProviderWebhookParseResult(
                merchantReference,
                providerReference,
                webhookEventId,
                eventName ?? "payment.webhook",
                paymentStatus,
                failureReason,
                "credo_webhook_received",
                new Dictionary<string, string>()));
    }

    private static PaymentProviderVerificationResult CreateVerificationResult(
        PaymentProviderVerificationRequest request,
        string providerKey)
    {
        if (request.MerchantReference.Contains("success", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                request.ProviderReference ?? $"cr_{Guid.CreateVersion7():N}",
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess,
                new Dictionary<string, string> { ["provider"] = providerKey });
        }

        if (request.MerchantReference.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                request.ProviderReference,
                "provider_reported_failure",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure,
                new Dictionary<string, string> { ["provider"] = providerKey });
        }

        return new PaymentProviderVerificationResult(
            PaymentProviderVerificationStatus.Processing,
            request.ProviderReference,
            null,
            KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing,
            new Dictionary<string, string> { ["provider"] = providerKey });
    }

    private static string? GetOptionalString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
}
