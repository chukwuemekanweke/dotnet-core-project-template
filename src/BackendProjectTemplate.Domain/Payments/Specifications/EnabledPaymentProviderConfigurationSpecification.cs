using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class EnabledPaymentProviderConfigurationSpecification : Specification<PaymentProviderConfiguration>
{
    public EnabledPaymentProviderConfigurationSpecification(Guid paymentProviderId, Guid currencyId, PaymentIntent paymentIntent)
    {
        Where(configuration =>
            configuration.PaymentProviderId == paymentProviderId &&
            configuration.CurrencyId == currencyId &&
            configuration.PaymentIntent == paymentIntent &&
            configuration.IsEnabled);
    }
}
