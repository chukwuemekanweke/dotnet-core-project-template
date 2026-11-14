using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class RecurringPaymentAuthorizationByCustomerSpecification : Specification<RecurringPaymentAuthorization>
{
    public RecurringPaymentAuthorizationByCustomerSpecification(Guid paymentProviderId, string providerCustomerReference)
    {
        Where(authorization =>
            authorization.PaymentProviderId == paymentProviderId &&
            authorization.ProviderCustomerReference == providerCustomerReference);
        EnableTracking();
    }
}
