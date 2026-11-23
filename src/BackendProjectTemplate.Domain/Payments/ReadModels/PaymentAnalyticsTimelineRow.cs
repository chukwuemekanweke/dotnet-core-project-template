namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsTimelineRow(
    DateOnly PeriodStart,
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
