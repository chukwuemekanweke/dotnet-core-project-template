using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed class BachsPaymentProviderService(
    IBachsClient client,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IOptions<BachsOptions> options) : IPaymentProviderService, IRecurringPaymentProvider
{
    private readonly BachsOptions _options = options.Value;

    public string ProviderKey => PaymentProviderKeys.Bachs;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PaymentIntent != PaymentIntent.WalletTopUp &&
            (request.PaymentIntent != PaymentIntent.Subscription || !request.IsTrustedSubscriptionPurchase))
        {
            throw new InvalidOperationException("Bachs subscription payments require the trusted subscription purchase flow.");
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

        string? customerId = null;
        if (request.SavePaymentMethod)
        {
            var customer = await client.CreateCustomerAsync(
                new BachsCreateCustomerRequest(
                    email,
                    string.IsNullOrWhiteSpace(customerName) ? null : customerName,
                    new Dictionary<string, string>
                    {
                        ["stakeholder_id"] = request.StakeholderId.ToString(),
                        ["tenant_id"] = request.TenantId.ToString()
                    }),
                $"{request.MerchantReference}-customer",
                cancellationToken);
            customerId = NormalizeRequired(customer.CustomerId, "Bachs create customer response did not contain a customer id.");
        }

        var response = await client.CreateCheckoutSessionAsync(
            new BachsCreateCheckoutSessionRequest(
                new BachsCheckoutPricing(
                    request.CurrencyCode.ToUpperInvariant(),
                    BachsAmountConverter.ToMajorUnitString(request.Amount, request.CurrencyCode)),
                request.SavePaymentMethod
                    ? new BachsCheckoutCustomer(null, null, customerId)
                    : new BachsCheckoutCustomer(email, string.IsNullOrWhiteSpace(customerName) ? null : customerName),
                NormalizeOptional(_options.SuccessUrl),
                NormalizeOptional(_options.CancelUrl),
                request.MerchantReference,
                new Dictionary<string, string>
                {
                    ["merchant_reference"] = request.MerchantReference,
                    ["payment_intent"] = request.PaymentIntent.ToString(),
                    ["stakeholder_id"] = request.StakeholderId.ToString()
                },
                _options.CheckoutExpirationMinutes,
                request.SavePaymentMethod),
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
            },
            customerId is null ? null : new RecurringPaymentSetupResult(customerId));
    }

    public async Task<RecurringChargeResult> ChargeAsync(
        RecurringChargeRequest request,
        CancellationToken cancellationToken)
    {
        EnsureRecurringCurrency(request.CurrencyCode);
        var response = await client.CreateChargeAsync(
            new BachsCreateChargeRequest(
                request.ProviderCustomerReference,
                request.PaymentMethodReference,
                BachsAmountConverter.ToMajorUnitString(request.Amount, request.CurrencyCode),
                request.CurrencyCode.ToUpperInvariant(),
                "Subscription renewal",
                request.MerchantReference,
                request.Metadata),
            request.IdempotencyKey,
            cancellationToken);

        return MapCharge(response, request.MerchantReference, request.Amount, request.CurrencyCode);
    }

    public async Task<RecurringChargeResult> GetChargeAsync(
        RecurringChargeVerificationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureRecurringCurrency(request.CurrencyCode);
        var response = await client.GetPaymentAsync(request.ProviderReference, cancellationToken);
        return MapCharge(response, request.MerchantReference, request.Amount, request.CurrencyCode);
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            return Processing(request.ProviderReference);
        }

        if (request.IsRecurringCharge)
        {
            var charge = await GetChargeAsync(
                new RecurringChargeVerificationRequest(
                    request.ProviderReference,
                    request.MerchantReference,
                    request.Amount,
                    request.CurrencyCode),
                cancellationToken);
            return charge.Status switch
            {
                RecurringChargeStatus.Succeeded => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Succeeded,
                    charge.ProviderReference,
                    null,
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess),
                RecurringChargeStatus.Failed => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    charge.ProviderReference,
                    charge.FailureReason,
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure),
                _ => Processing(charge.ProviderReference)
            };
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

        if ((EqualsValue(response.Status, BachsCheckoutStatuses.Expired) ||
             EqualsValue(response.Status, BachsCheckoutStatuses.Cancelled)) &&
            TryGetUnderpaidAmount(response.Charge, request) is { } amountPaid)
        {
            // An underpaid crypto charge stays payable until the checkout closes, so it is only settled here.
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Underpaid,
                providerReference,
                "provider_reported_underpayment",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedUnderpayment,
                amountPaid);
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

    private static decimal? TryGetUnderpaidAmount(
        BachsCheckoutCharge? charge,
        PaymentProviderVerificationRequest request)
    {
        if (charge is null ||
            !EqualsValue(charge.Status, BachsPaymentStatuses.Underpaid) ||
            string.IsNullOrWhiteSpace(charge.AmountPaid) ||
            !string.Equals(NormalizeOptional(charge.Currency), request.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var amountPaid = BachsAmountConverter.ToMinorUnits(charge.AmountPaid, request.CurrencyCode);
            return amountPaid > 0 && amountPaid < request.Amount ? amountPaid : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
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

    private static RecurringChargeResult MapCharge(
        BachsChargeResponse response,
        string merchantReference,
        decimal amount,
        string currencyCode)
    {
        var providerReference = NormalizeRequired(response.PaymentId, "Bachs charge response did not contain a payment id.");
        if (!string.Equals(NormalizeOptional(response.Reference), merchantReference, StringComparison.Ordinal) ||
            !string.Equals(NormalizeOptional(response.Currency), currencyCode, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(response.Amount) ||
            BachsAmountConverter.ToMinorUnits(response.Amount, currencyCode) != amount)
        {
            return new RecurringChargeResult(
                RecurringChargeStatus.Failed,
                providerReference,
                "provider_identity_mismatch",
                "Bachs charge response did not match the renewal payment.");
        }

        if (EqualsValue(response.Status, BachsPaymentStatuses.Succeeded))
        {
            return new RecurringChargeResult(RecurringChargeStatus.Succeeded, providerReference, null, null);
        }

        if (EqualsValue(response.Status, "failed") ||
            EqualsValue(response.Status, "expired") ||
            EqualsValue(response.Status, "cancelled"))
        {
            return new RecurringChargeResult(
                RecurringChargeStatus.Failed,
                providerReference,
                NormalizeOptional(response.FailureCode) ?? "provider_reported_failure",
                NormalizeOptional(response.FailureReason) ?? "Bachs reported that the recurring charge failed.");
        }

        return new RecurringChargeResult(RecurringChargeStatus.Processing, providerReference, null, null);
    }

    private static void EnsureRecurringCurrency(string currencyCode)
    {
        if (!string.Equals(currencyCode, "NGN", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(currencyCode, "USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Bachs recurring charges do not support '{currencyCode}'.");
        }
    }

    private static string NormalizeRequired(string? value, string errorMessage) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(errorMessage) : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
