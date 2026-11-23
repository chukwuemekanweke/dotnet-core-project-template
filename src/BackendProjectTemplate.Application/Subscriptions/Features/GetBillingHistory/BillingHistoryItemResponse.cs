namespace BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;

public sealed record BillingHistoryItemResponse(
    Guid PaymentTransactionId,
    Guid SubscriptionId,
    string PlanCode,
    string PlanName,
    DateTimeOffset? BillingPeriodStartUtc,
    DateTimeOffset? BillingPeriodEndUtc,
    decimal Amount,
    string CurrencyCode,
    string PaymentStatus,
    string PaymentProviderName,
    DateTimeOffset? PaidAtUtc,
    string MerchantReference,
    DateTimeOffset CreatedAtUtc);
