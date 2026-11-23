namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;

public sealed record GetSubscriptionPurchaseStatusResponse(
    Guid PurchaseId,
    Guid SubscriptionId,
    string MerchantReference,
    string Status,
    string PaymentStatus,
    string SubscriptionStatus,
    string FulfillmentStatus,
    bool IsTerminal,
    bool ShouldContinuePolling,
    decimal Amount,
    string CurrencyCode,
    string PaymentProviderName,
    string PaymentMethodType,
    DateTimeOffset? PaymentExpiresAtUtc,
    DateTimeOffset? PaymentCompletedAtUtc,
    DateTimeOffset? FulfilledAtUtc,
    DateTimeOffset? FulfillmentBlockedAtUtc,
    string? FulfillmentBlockedReason,
    DateTimeOffset? CurrentPeriodEndUtc);
