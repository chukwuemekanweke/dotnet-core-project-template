namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsCurrencyRow(
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    long SuccessfulCount,
    long UnderpaidCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume,
    decimal UnderpaidVolume);
