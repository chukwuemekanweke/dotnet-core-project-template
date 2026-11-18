using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public interface IPaymentAdministrationReadModelRepository
{
    Task<AdminPaymentListPage> ListPaymentsAsync(AdminPaymentListRequest request, CancellationToken cancellationToken);

    Task<AdminPaymentDetailReadModel?> GetPaymentDetailAsync(
        Guid paymentTransactionId,
        int webhookHistoryLimit,
        CancellationToken cancellationToken);
}

public sealed record AdminPaymentListRequest(
    Guid? TenantId,
    Guid? StakeholderId,
    Guid? PaymentProviderId,
    Guid? CurrencyId,
    PaymentIntent? PaymentIntent,
    PaymentStatus? PaymentStatus,
    PaymentMethodType? PaymentMethodType,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc,
    string? Search,
    DateTimeOffset? CursorCreatedAtUtc,
    Guid? CursorPaymentTransactionId,
    int Limit);

public sealed record AdminPaymentListPage(IReadOnlyList<AdminPaymentListItemReadModel> Items, bool HasMore);

public sealed record AdminPaymentListItemReadModel(
    Guid TransactionId,
    string MerchantReference,
    string? ProviderReference,
    Guid TenantId,
    Guid StakeholderId,
    string? StakeholderDisplayName,
    Guid PaymentProviderId,
    string PaymentProviderName,
    string PaymentProviderKey,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    PaymentIntent PaymentIntent,
    PaymentMethodType PaymentMethodType,
    PaymentStatus PaymentStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc);

public sealed record AdminPaymentDetailReadModel(
    AdminPaymentDetailCoreReadModel Payment,
    IReadOnlyList<AdminPaymentWebhookReadModel> Webhooks,
    AdminPaymentFulfillmentReadModel Fulfillment);

public sealed record AdminPaymentDetailCoreReadModel(
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
    PaymentIntent PaymentIntent,
    PaymentMethodType PaymentMethodType,
    PaymentStatus PaymentStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastStatusCheckAtUtc,
    string? StatusChangeReason,
    string? FailureReason);

public sealed record AdminPaymentWebhookReadModel(
    Guid WebhookId,
    string WebhookEventName,
    string? WebhookEventId,
    WebhookProcessingStatus WebhookProcessingStatus,
    SignatureValidationStatus SignatureValidationStatus,
    string? StatusChangeReason,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? ProcessedAtUtc);

public sealed record AdminPaymentFulfillmentReadModel(
    string Type,
    string Status,
    Guid? ReferenceId,
    DateTimeOffset? FulfilledAtUtc,
    string? Reason);
