using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsIntentRow(
    PaymentIntent PaymentIntent,
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    long SuccessfulCount,
    long UnsuccessfulCount,
    long UnderpaidCount,
    long ProcessingCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume,
    decimal UnderpaidVolume);
