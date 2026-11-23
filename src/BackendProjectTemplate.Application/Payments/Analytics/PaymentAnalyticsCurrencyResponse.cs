namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsCurrencyResponse(
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    long UnderpaidCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume,
    decimal UnderpaidVolume,
    decimal AverageSuccessfulTransactionValue);
