namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsSummaryResponse(
    long TransactionCount,
    long SuccessfulCount,
    long UnsuccessfulCount,
    long UnderpaidCount,
    long ProcessingCount,
    decimal SuccessRate,
    IReadOnlyList<PaymentAnalyticsCurrencyResponse> Currencies);
