using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderInitiationRequest(
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent,
    Guid StakeholderId,
    Guid TenantId,
    Guid CountryId);
