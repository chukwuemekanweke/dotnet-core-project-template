using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record CustomerBillingHistoryItemReadModel(
    Guid PaymentTransactionId,
    Guid SubscriptionId,
    string PlanCode,
    string PlanName,
    DateTimeOffset? BillingPeriodStartUtc,
    DateTimeOffset? BillingPeriodEndUtc,
    decimal Amount,
    string CurrencyCode,
    PaymentStatus PaymentStatus,
    string PaymentProviderName,
    DateTimeOffset? PaidAtUtc,
    string MerchantReference,
    DateTimeOffset CreatedAtUtc);
