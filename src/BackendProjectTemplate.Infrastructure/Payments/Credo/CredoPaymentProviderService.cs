using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal sealed class CredoPaymentProviderService(
    ICredoClient client,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    TimeProvider timeProvider) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.Credo;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(request.StakeholderId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Unable to resolve stakeholder '{request.StakeholderId}' for Credo payment initiation.");

        var response = await client.InitializeTransactionAsync(
            new CredoInitializeTransactionRequest(
                ConvertAmount(request.Amount),
                string.IsNullOrWhiteSpace(stakeholder.EmailAddress)
                    ? throw new InvalidOperationException(
                        $"Stakeholder '{stakeholder.StakeholderId}' does not have an email for Credo payment initiation.")
                    : stakeholder.EmailAddress.Trim(),
                null,
                stakeholder.FirstName,
                stakeholder.LastName,
                request.CurrencyCode,
                request.MerchantReference,
                $"{request.PaymentIntent} payment"),
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var providerReference = NormalizeRequired(
            response.CredoReference,
            "Credo initialize transaction response did not contain a credo reference.");
        var merchantReference = NormalizeRequired(
            response.Reference,
            "Credo initialize transaction response did not contain a merchant reference.");
        var paymentLink = NormalizeRequired(
            response.AuthorizationUrl,
            "Credo initialize transaction response did not contain an authorization URL.");

        return new PaymentProviderInitiationResult(
            providerReference,
            ProviderKey,
            PaymentMethodType.PaymentLink,
            now.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["paymentLink"] = paymentLink,
                ["providerReference"] = providerReference,
                ["merchantReference"] = merchantReference,
                ["crn"] = response.Crn ?? string.Empty
            });
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await client.VerifyTransactionAsync(request.MerchantReference, cancellationToken);
        var metadata = CreateVerificationMetadata(response);

        return response.Status switch
        {
            CredoTransactionStatuses.Successful or CredoTransactionStatuses.SettleQueued or CredoTransactionStatuses.Settled
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Succeeded,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    null,
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess,
                    metadata),
            CredoTransactionStatuses.Failed or CredoTransactionStatuses.Declined or CredoTransactionStatuses.FailedAged or CredoTransactionStatuses.Abandoned
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure,
                    metadata),
            _ => new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing,
                metadata)
        };
    }

    public Task<PaymentProviderWebhookValidationResult> ValidateWebhookAsync(
        PaymentProviderWebhookValidationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.NotApplicable, "signature_not_configured"));

    private static long ConvertAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Credo amount must be greater than zero.");
        }

        if (decimal.Truncate(amount) != amount)
        {
            throw new InvalidOperationException("Credo amount must be provided in the lowest currency unit.");
        }

        return decimal.ToInt64(amount);
    }

    private static Dictionary<string, string> CreateVerificationMetadata(CredoVerifyTransactionResponse response)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider"] = PaymentProviderKeys.Credo,
            ["status"] = response.Status.ToString(),
            ["debitedAmount"] = response.DebitedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["transactionAmount"] = response.TransAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["settlementAmount"] = response.SettlementAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(response.CurrencyCode))
        {
            metadata["currencyCode"] = response.CurrencyCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(response.BusinessRef))
        {
            metadata["merchantReference"] = response.BusinessRef.Trim();
        }

        if (!string.IsNullOrWhiteSpace(response.TransRef))
        {
            metadata["providerReference"] = response.TransRef.Trim();
        }

        if (response.Metadata is not null)
        {
            foreach (var item in response.Metadata)
            {
                if (string.IsNullOrWhiteSpace(item.InsightTag) || string.IsNullOrWhiteSpace(item.InsightTagValue))
                {
                    continue;
                }

                metadata[$"metadata.{item.InsightTag.Trim()}"] = item.InsightTagValue.Trim();
            }
        }

        return metadata;
    }

    private static string NormalizeRequired(string? value, string errorMessage) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(errorMessage)
            : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeFailureReason(string? message, int status) =>
        string.IsNullOrWhiteSpace(message)
            ? $"provider_status_{status}"
            : message.Trim();
}
