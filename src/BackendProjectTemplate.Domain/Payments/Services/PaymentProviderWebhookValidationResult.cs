using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderWebhookValidationResult(
    SignatureValidationStatus SignatureValidationStatus,
    string? StatusChangeReason);
