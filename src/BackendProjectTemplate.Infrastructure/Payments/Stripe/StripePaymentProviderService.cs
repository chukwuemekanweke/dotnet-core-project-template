using System.Text.Json;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.Infrastructure.Payments.Stripe;

internal sealed class StripePaymentProviderService(
    IStripeClient client,
    TimeProvider timeProvider) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.Stripe;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        await client.CreateCheckoutSessionAsync(
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
        var providerReference = $"st_{Guid.CreateVersion7():N}";

        return new PaymentProviderInitiationResult(
            providerReference,
            PaymentStatus.AwaitingCustomerAction,
            PaymentMethodType.PaymentLink,
            now.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["paymentLink"] = $"https://checkout.stripe.local/pay/{providerReference}",
                ["providerReference"] = providerReference
            },
            new Dictionary<string, string>
            {
                ["provider"] = PaymentProviderKeys.Stripe
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
        var status = root.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        return Task.FromResult(
            new PaymentProviderWebhookParseResult(
                GetOptionalString(root, "merchantReference"),
                GetOptionalString(root, "providerReference"),
                GetOptionalString(root, "eventId"),
                GetOptionalString(root, "eventName") ?? "payment.webhook",
                MapStatus(status),
                status is not null && status.Equals("failed", StringComparison.OrdinalIgnoreCase) ? "provider_reported_failure" : null,
                "stripe_webhook_received",
                new Dictionary<string, string>()));
    }

    private static PaymentProviderVerificationResult CreateVerificationResult(
        PaymentProviderVerificationRequest request,
        string providerKey)
    {
        if (request.MerchantReference.Contains("success", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentStatus.Succeeded,
                request.ProviderReference ?? $"st_{Guid.CreateVersion7():N}",
                null,
                "reconciliation_confirmed_success",
                new Dictionary<string, string> { ["provider"] = providerKey });
        }

        if (request.MerchantReference.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentStatus.Failed,
                request.ProviderReference,
                "provider_reported_failure",
                "reconciliation_confirmed_failure",
                new Dictionary<string, string> { ["provider"] = providerKey });
        }

        return new PaymentProviderVerificationResult(
            PaymentStatus.Processing,
            request.ProviderReference,
            null,
            "reconciliation_still_processing",
            new Dictionary<string, string> { ["provider"] = providerKey });
    }

    private static PaymentStatus? MapStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "succeeded" or "success" => PaymentStatus.Succeeded,
            "failed" => PaymentStatus.Failed,
            "processing" => PaymentStatus.Processing,
            "pending" or "awaiting_customer_action" => PaymentStatus.AwaitingCustomerAction,
            _ => null
        };

    private static string? GetOptionalString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
}
