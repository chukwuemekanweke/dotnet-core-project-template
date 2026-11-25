namespace BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;

public sealed record PaymentProviderResponse(
    Guid Id,
    string ProviderName,
    string ProviderKey,
    bool IsActive);
