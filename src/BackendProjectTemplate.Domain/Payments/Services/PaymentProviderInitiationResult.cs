using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderInitiationResult(
    string ProviderReference,
    PaymentStatus PaymentStatus,
    PaymentMethodType PaymentMethodType,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyDictionary<string, string> InstructionFields,
    IReadOnlyDictionary<string, string> Metadata);
