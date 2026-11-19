namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsSummaryReadModel(
    long TransactionCount,
    long SuccessfulCount,
    long UnsuccessfulCount,
    long ProcessingCount,
    IReadOnlyList<PaymentAnalyticsCurrencyRow> Currencies);
