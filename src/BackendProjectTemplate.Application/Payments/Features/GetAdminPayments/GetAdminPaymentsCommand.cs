using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;

public sealed record GetAdminPaymentsCommand(
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
    string? Cursor,
    int Limit);
