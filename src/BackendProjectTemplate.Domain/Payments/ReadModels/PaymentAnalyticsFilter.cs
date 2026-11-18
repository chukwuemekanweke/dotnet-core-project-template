using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsFilter(
    DateTimeOffset FromUtc,
    DateTimeOffset ToExclusiveUtc,
    Guid? PaymentProviderId,
    Guid? CurrencyId,
    PaymentIntent? PaymentIntent,
    PaymentStatus? PaymentStatus,
    Guid? TenantId);
