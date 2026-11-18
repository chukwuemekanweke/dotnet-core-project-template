using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record PaymentAnalyticsStatusRow(
    PaymentStatus PaymentStatus,
    Guid CurrencyId,
    string CurrencyCode,
    long TransactionCount,
    decimal TotalAmount);
