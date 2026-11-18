namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;

public sealed record GetAdminPaymentDetailResponse(
    Guid TransactionId,
    string MerchantReference,
    string? ProviderReference,
    Guid TenantId,
    Guid StakeholderId,
    string? StakeholderDisplayName,
    Guid InitiatedByUserId,
    Guid PaymentProviderId,
    string PaymentProviderName,
    string PaymentProviderKey,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    string PaymentIntent,
    string PaymentMethodType,
    string PaymentStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastStatusCheckAtUtc,
    string? StatusChangeReason,
    string? FailureReason,
    AdminPaymentCurrentStatusResponse CurrentStatus,
    IReadOnlyList<AdminPaymentWebhookResponse> Webhooks,
    AdminPaymentReconciliationResponse Reconciliation,
    AdminPaymentFulfillmentResponse Fulfillment);

public sealed record AdminPaymentCurrentStatusResponse(string Status, string? Reason, DateTimeOffset UpdatedAtUtc);

public sealed record AdminPaymentWebhookResponse(
    Guid WebhookId,
    string WebhookEventName,
    string? WebhookEventId,
    string WebhookProcessingStatus,
    string SignatureValidationStatus,
    string? StatusChangeReason,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? ProcessedAtUtc);

public sealed record AdminPaymentReconciliationResponse(DateTimeOffset? LastStatusCheckAtUtc);

public sealed record AdminPaymentFulfillmentResponse(
    string Type,
    string Status,
    Guid? ReferenceId,
    DateTimeOffset? FulfilledAtUtc,
    string? Reason);
