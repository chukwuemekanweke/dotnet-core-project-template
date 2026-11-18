namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments;

public sealed record ListAdminPaymentsRequest(
    Guid? TenantId = null,
    Guid? StakeholderId = null,
    Guid? PaymentProviderId = null,
    Guid? CurrencyId = null,
    string? PaymentIntent = null,
    string? PaymentStatus = null,
    string? PaymentMethodType = null,
    DateTimeOffset? CreatedFromUtc = null,
    DateTimeOffset? CreatedToUtc = null,
    string? Search = null,
    string? Cursor = null,
    int Limit = 20);
