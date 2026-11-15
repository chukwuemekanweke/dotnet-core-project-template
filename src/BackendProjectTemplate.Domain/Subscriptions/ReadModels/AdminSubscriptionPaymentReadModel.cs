using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionPaymentReadModel(
    Guid PaymentTransactionId,
    string MerchantReference,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    PaymentStatus PaymentStatus,
    Guid? RenewalId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc);
