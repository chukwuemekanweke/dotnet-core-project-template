namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed record PaymentAnalyticsStatusResponse(
    string PaymentStatus,
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    decimal TotalAmount,
    decimal Percentage);
