using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderVerificationResult(
    PaymentStatus PaymentStatus,
    string? ProviderReference,
    string? FailureReason,
    string? StatusChangeReason,
    IReadOnlyDictionary<string, string> Metadata);
