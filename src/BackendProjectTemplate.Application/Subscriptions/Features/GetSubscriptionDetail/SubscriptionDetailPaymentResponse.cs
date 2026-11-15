namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailPaymentResponse(
    Guid PaymentTransactionId,
    string MerchantReference,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    string PaymentStatus,
    Guid? RenewalId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc);
