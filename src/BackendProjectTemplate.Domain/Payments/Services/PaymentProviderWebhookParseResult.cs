using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderWebhookParseResult(
    string? MerchantReference,
    string? ProviderReference,
    string? WebhookEventId,
    string WebhookEventName,
    PaymentStatus? PaymentStatus,
    string? FailureReason,
    string? StatusChangeReason,
    IReadOnlyDictionary<string, string> Metadata);
