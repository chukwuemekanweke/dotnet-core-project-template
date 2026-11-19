namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsCurrencyResponse(
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume,
    decimal AverageSuccessfulTransactionValue);
