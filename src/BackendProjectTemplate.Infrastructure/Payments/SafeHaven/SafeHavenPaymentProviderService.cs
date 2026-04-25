using System.Text.Json;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed class SafeHavenPaymentProviderService(
    ISafeHavenClient client,
    TimeProvider timeProvider) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.SafeHaven;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        await client.CreateVirtualAccountPaymentAsync(
            new
            {
                request.MerchantReference,
                request.Amount,
                request.CurrencyCode,
                request.StakeholderId,
                request.TenantId,
                request.CountryId
            },
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var providerReference = $"sh_{Guid.CreateVersion7():N}";

        return new PaymentProviderInitiationResult(
            providerReference,
            PaymentMethodType.BankTransfer,
            now.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["accountNumber"] = "1234567890",
                ["bankName"] = "SafeHaven Demo Bank",
                ["accountName"] = "Backend Project Template",
                ["providerReference"] = providerReference
            },
            new Dictionary<string, string>
            {
                ["provider"] = PaymentProviderKeys.SafeHaven
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
        var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;

        if (!string.Equals(type, "transfer.successful", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                new PaymentProviderWebhookParseResult(
                    null,
                    null,
                    null,
                    type ?? "safehaven.webhook",
                    null,
                    null,
                    "unsupported_safehaven_webhook_type",
                    new Dictionary<string, string>()));
        }

        return Task.FromResult(
            new PaymentProviderWebhookParseResult(
                GetOptionalString(data, "merchantReference"),
                GetOptionalString(data, "providerReference"),
                $"{GetOptionalString(data, "merchantReference")}:{type}",
                type ?? "safehaven.webhook",
                PaymentStatus.Succeeded,
                null,
                "safehaven_transfer_successful",
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
                request.ProviderReference ?? $"sh_{Guid.CreateVersion7():N}",
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

    private static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
}
