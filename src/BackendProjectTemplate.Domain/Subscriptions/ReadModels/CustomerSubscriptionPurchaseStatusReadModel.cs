using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record CustomerSubscriptionPurchaseStatusReadModel(
    Guid PurchaseId,
    Guid SubscriptionId,
    string MerchantReference,
    PaymentStatus PaymentStatus,
    PaymentMethodType PaymentMethodType,
    string PaymentProviderName,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset? PaymentExpiresAtUtc,
    DateTimeOffset? PaymentCompletedAtUtc,
    DateTimeOffset? FulfilledAtUtc,
    DateTimeOffset? FulfillmentBlockedAtUtc,
    string? FulfillmentBlockedReason,
    SubscriptionStatus SubscriptionStatus,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
