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
            ProviderKey,
            PaymentMethodType.PaymentLink,
            now.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["paymentLink"] = $"https://checkout.credo.local/pay/{providerReference}",
                ["providerReference"] = providerReference
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

}
