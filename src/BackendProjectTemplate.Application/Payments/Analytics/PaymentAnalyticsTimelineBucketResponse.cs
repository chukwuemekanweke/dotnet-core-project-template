namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsTimelineBucketResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
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
