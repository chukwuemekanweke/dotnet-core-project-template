namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsCurrencyRow(
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    long SuccessfulCount,
    decimal AttemptedVolume,
    decimal SuccessfulVolume,
    decimal FailedVolume,
    decimal ProcessingVolume,
    decimal CancelledVolume,
    decimal ExpiredVolume);
