using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderVerificationRequest(
    string MerchantReference,
    string? ProviderReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent);
