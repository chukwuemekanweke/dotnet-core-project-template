using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed class BachsPaymentProviderService(
    IBachsClient client,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IOptions<BachsOptions> options) : IPaymentProviderService
{
    private readonly BachsOptions _options = options.Value;

    public string ProviderKey => PaymentProviderKeys.Bachs;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PaymentIntent != PaymentIntent.WalletTopUp)
        {
            throw new InvalidOperationException("The Bachs integration currently supports wallet top-up payments only.");
        }

        if (_options.CheckoutExpirationMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Bachs checkout expiration must be between 1 and 1440 minutes.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(
            request.StakeholderId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Unable to resolve stakeholder '{request.StakeholderId}' for Bachs payment initiation.");
        var email = string.IsNullOrWhiteSpace(stakeholder.EmailAddress)
            ? throw new InvalidOperationException(
                $"Stakeholder '{stakeholder.StakeholderId}' does not have an email for Bachs payment initiation.")
            : stakeholder.EmailAddress.Trim();
        var customerName = string.Join(
            ' ',
            new[] { stakeholder.FirstName, stakeholder.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

        var response = await client.CreateCheckoutSessionAsync(
            new BachsCreateCheckoutSessionRequest(
                new BachsCheckoutPricing(
                    request.CurrencyCode.ToUpperInvariant(),
                    BachsAmountConverter.ToMajorUnitString(request.Amount, request.CurrencyCode)),
                new BachsCheckoutCustomer(email, string.IsNullOrWhiteSpace(customerName) ? null : customerName),
                NormalizeOptional(_options.SuccessUrl),
                NormalizeOptional(_options.CancelUrl),
                request.MerchantReference,
                new Dictionary<string, string>
                {
                    ["merchant_reference"] = request.MerchantReference,
                    ["payment_intent"] = request.PaymentIntent.ToString(),
                    ["stakeholder_id"] = request.StakeholderId.ToString()
                },
                _options.CheckoutExpirationMinutes),
            request.MerchantReference,
            cancellationToken);

        var checkoutId = NormalizeRequired(
            response.CheckoutId,
            "Bachs create checkout response did not contain a checkout id.");
        var checkoutUrl = NormalizeRequired(
            response.CheckoutUrl,
            "Bachs create checkout response did not contain a checkout URL.");
        if (!Uri.TryCreate(checkoutUrl, UriKind.Absolute, out var parsedCheckoutUrl) ||
            parsedCheckoutUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Bachs create checkout response contained an invalid checkout URL.");
        }

        var responseReference = NormalizeRequired(
            response.Reference,
            "Bachs create checkout response did not contain the merchant reference.");
        if (!string.Equals(responseReference, request.MerchantReference, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bachs create checkout response contained a mismatched merchant reference.");
        }

        return new PaymentProviderInitiationResult(
            checkoutId,
            ProviderKey,
            PaymentMethodType.PaymentLink,
            response.ExpiresAt,
            new Dictionary<string, string>
            {
                [BachsKnownKeys.PaymentInstruction.PaymentLink] = checkoutUrl,
                [BachsKnownKeys.PaymentInstruction.ProviderReference] = checkoutId,
                [BachsKnownKeys.PaymentInstruction.MerchantReference] = responseReference
            });
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            return Processing(request.ProviderReference);
        }

        var response = await client.GetCheckoutSessionAsync(request.ProviderReference, cancellationToken);
        var providerReference = NormalizeOptional(response.CheckoutId) ?? request.ProviderReference;
        var mismatch = ValidateTransactionIdentity(response, request);
        if (mismatch is not null)
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                providerReference,
                mismatch,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure);
        }

        if (EqualsValue(response.Status, BachsCheckoutStatuses.Completed) &&
            EqualsValue(response.PaymentStatus, BachsPaymentStatuses.Succeeded))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                providerReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess);
        }

        if (EqualsValue(response.Status, BachsCheckoutStatuses.Expired))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Expired,
                providerReference,
                "provider_reported_expired",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedExpired);
        }

        if (EqualsValue(response.Status, BachsCheckoutStatuses.Cancelled))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                providerReference,
                "provider_reported_cancelled",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure);
        }

        // A failed collection does not close a Bachs checkout; the buyer may retry until expiry.
        return Processing(providerReference);
    }

    private static string? ValidateTransactionIdentity(
        BachsCheckoutSessionResponse response,
        PaymentProviderVerificationRequest request)
    {
        if (!string.Equals(NormalizeOptional(response.Reference), request.MerchantReference, StringComparison.Ordinal))
        {
            return "provider_merchant_reference_mismatch";
        }

        if (!string.Equals(NormalizeOptional(response.Currency), request.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return "provider_currency_mismatch";
        }

        try
        {
            if (string.IsNullOrWhiteSpace(response.Amount) ||
                BachsAmountConverter.ToMinorUnits(response.Amount, request.CurrencyCode) != request.Amount)
            {
                return "provider_amount_mismatch";
            }
        }
        catch (InvalidOperationException)
        {
            return "provider_amount_mismatch";
        }

        return null;
    }

    private static PaymentProviderVerificationResult Processing(string? providerReference) =>
        new(
            PaymentProviderVerificationStatus.Processing,
            providerReference,
            null,
            KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing);

    private static bool EqualsValue(string? value, string expected) =>
        string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRequired(string? value, string errorMessage) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(errorMessage) : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
