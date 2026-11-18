namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsIntentResponse(
    string PaymentIntent,
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    long SuccessfulCount,
    long UnsuccessfulCount,
    long ProcessingCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume,
    decimal SuccessRate,
    decimal AverageSuccessfulTransactionValue);
