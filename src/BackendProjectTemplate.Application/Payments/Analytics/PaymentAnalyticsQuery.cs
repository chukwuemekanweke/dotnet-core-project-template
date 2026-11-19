using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? PaymentProviderId,
    Guid? CurrencyId,
    PaymentIntent? PaymentIntent,
    PaymentStatus? PaymentStatus,
    Guid? TenantId);
