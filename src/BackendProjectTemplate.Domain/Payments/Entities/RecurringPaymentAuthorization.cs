using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class RecurringPaymentAuthorization : Entity, IAggregateRoot
{
    public const int MaxReferenceLength = 500;

    private RecurringPaymentAuthorization()
    {
    }

    private RecurringPaymentAuthorization(
        Guid tenantId,
        Guid billingAccountId,
        Guid paymentProviderId,
        string authorizationReference,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        TenantId = tenantId;
        BillingAccountId = billingAccountId;
        PaymentProviderId = paymentProviderId;
        AuthorizationReference = NormalizeReference(authorizationReference);
        Status = RecurringPaymentAuthorizationStatus.Active;
        AuthorizedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid PaymentProviderId { get; private set; }
    public string AuthorizationReference { get; private set; } = string.Empty;
    public RecurringPaymentAuthorizationStatus Status { get; private set; }
    public DateTimeOffset AuthorizedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public static RecurringPaymentAuthorization Create(
        Guid tenantId,
        Guid billingAccountId,
        Guid paymentProviderId,
        string authorizationReference,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc = null) =>
        new(tenantId, billingAccountId, paymentProviderId, authorizationReference, createdAtUtc, expiresAtUtc);

    public bool IsUsableAt(DateTimeOffset utcNow) =>
        Status == RecurringPaymentAuthorizationStatus.Active &&
        (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        Status = RecurringPaymentAuthorizationStatus.Revoked;
        RevokedAtUtc = revokedAtUtc;
    }

    private static string NormalizeReference(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > MaxReferenceLength)
        {
            throw new ArgumentException($"Authorization reference must contain at most {MaxReferenceLength} characters.", nameof(value));
        }

        return normalized;
    }
}
