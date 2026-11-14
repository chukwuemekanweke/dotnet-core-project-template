using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class UsableRecurringPaymentAuthorizationSpecification : Specification<RecurringPaymentAuthorization>
{
    public UsableRecurringPaymentAuthorizationSpecification(
        Guid tenantId,
        Guid billingAccountId,
        Guid currencyId,
        DateTimeOffset utcNow)
    {
        Where(authorization =>
            authorization.TenantId == tenantId &&
            authorization.BillingAccountId == billingAccountId &&
            authorization.CurrencyId == currencyId &&
            authorization.Status == RecurringPaymentAuthorizationStatus.Active &&
            authorization.AuthorizationReference != null &&
            (!authorization.ExpiresAtUtc.HasValue || authorization.ExpiresAtUtc.Value > utcNow));
        ApplyOrderByDescending(authorization => authorization.AuthorizedAtUtc ?? authorization.ConsentedAtUtc);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
