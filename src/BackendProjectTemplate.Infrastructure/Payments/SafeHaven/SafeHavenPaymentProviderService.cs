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
        var response = await client.CreateVirtualAccountAsync(
            new SafeHavenCreateVirtualAccountRequest(
                ExternalReference: request.MerchantReference,
                AccountName: request.MerchantReference,
                Amount: request.Amount),
            cancellationToken);

        var virtualAccount = response.Data;
        var now = timeProvider.GetUtcNow();

        return new PaymentProviderInitiationResult(
            virtualAccount.Id,
            PaymentMethodType.BankTransfer,
            now.AddMinutes(15),
            new Dictionary<string, string>
            {
                ["accountNumber"] = virtualAccount.AccountNumber,
                ["bankName"] = virtualAccount.BankCode,
                ["accountName"] = virtualAccount.AccountName,
                ["providerReference"] = virtualAccount.Id
            },
            new Dictionary<string, string>
            {
                ["provider"] = PaymentProviderKeys.SafeHaven
            });
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing,
                new Dictionary<string, string> { ["provider"] = ProviderKey });
        }

        var response = await client.GetVirtualAccountAsync(request.ProviderReference, cancellationToken);

        if (response is null)
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing,
                new Dictionary<string, string> { ["provider"] = ProviderKey });
        }

        var virtualAccount = response.Data;

        if (string.Equals(virtualAccount.Status, "completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(virtualAccount.Status, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(virtualAccount.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess,
                new Dictionary<string, string> { ["provider"] = ProviderKey });
        }

        if (string.Equals(virtualAccount.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(virtualAccount.Status, "expired", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                request.ProviderReference,
                "provider_reported_failure",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure,
                new Dictionary<string, string> { ["provider"] = ProviderKey });
        }

        return new PaymentProviderVerificationResult(
            PaymentProviderVerificationStatus.Processing,
            request.ProviderReference,
            null,
            KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing,
            new Dictionary<string, string> { ["provider"] = ProviderKey });
    }

    public Task<PaymentProviderWebhookValidationResult> ValidateWebhookAsync(
        PaymentProviderWebhookValidationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.NotApplicable, "signature_not_configured"));
}
