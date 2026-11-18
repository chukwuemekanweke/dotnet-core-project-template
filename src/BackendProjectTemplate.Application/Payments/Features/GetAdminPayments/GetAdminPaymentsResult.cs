namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;

public sealed record GetAdminPaymentsResult(IReadOnlyList<AdminPaymentSummaryResponse> Items, string? NextCursor);

public sealed record AdminPaymentSummaryResponse(
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
    string PaymentIntent,
    string PaymentMethodType,
    string PaymentStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc);
