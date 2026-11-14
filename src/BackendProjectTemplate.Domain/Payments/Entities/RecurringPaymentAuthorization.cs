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
        string providerCustomerReference,
        Guid currencyId,
        Guid sourcePaymentTransactionId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset consentedAtUtc)
    {
        TenantId = tenantId;
        BillingAccountId = billingAccountId;
        PaymentProviderId = paymentProviderId;
        ProviderCustomerReference = NormalizeReference(providerCustomerReference);
        CurrencyId = currencyId;
        SourcePaymentTransactionId = sourcePaymentTransactionId;
        Status = RecurringPaymentAuthorizationStatus.Pending;
        ConsentedAtUtc = consentedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid BillingAccountId { get; private set; }
    public Guid PaymentProviderId { get; private set; }
    public string? AuthorizationReference { get; private set; }
    public string ProviderCustomerReference { get; private set; } = string.Empty;
    public Guid CurrencyId { get; private set; }
    public Guid SourcePaymentTransactionId { get; private set; }
    public RecurringPaymentAuthorizationStatus Status { get; private set; }
    public DateTimeOffset ConsentedAtUtc { get; private set; }
    public DateTimeOffset? AuthorizedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public static RecurringPaymentAuthorization Create(
        Guid tenantId,
        Guid billingAccountId,
        Guid paymentProviderId,
        string providerCustomerReference,
        Guid currencyId,
        Guid sourcePaymentTransactionId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset consentedAtUtc) =>
        new(
            tenantId,
            billingAccountId,
            paymentProviderId,
            providerCustomerReference,
            currencyId,
            sourcePaymentTransactionId,
            createdAtUtc,
            consentedAtUtc);

    public bool IsUsableAt(DateTimeOffset utcNow) =>
        Status == RecurringPaymentAuthorizationStatus.Active &&
        (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);

    public void Activate(string protectedAuthorizationReference, DateTimeOffset authorizedAtUtc, DateTimeOffset? expiresAtUtc = null)
    {
        if (Status is RecurringPaymentAuthorizationStatus.Revoked or RecurringPaymentAuthorizationStatus.Expired)
        {
            throw new InvalidOperationException("A revoked or expired recurring payment authorization cannot be activated.");
        }

        AuthorizationReference = NormalizeReference(protectedAuthorizationReference);
        Status = RecurringPaymentAuthorizationStatus.Active;
        AuthorizedAtUtc = authorizedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

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
