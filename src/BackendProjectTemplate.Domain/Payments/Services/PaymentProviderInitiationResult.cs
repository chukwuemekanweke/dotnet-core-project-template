using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderInitiationResult(
    string ProviderReference,
    string PaymentProvider,
    PaymentMethodType PaymentMethodType,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyDictionary<string, string> InstructionFields);
