using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class EnabledPaymentProviderConfigurationsForCurrencySpecification
    : Specification<PaymentProviderConfiguration>
{
    public EnabledPaymentProviderConfigurationsForCurrencySpecification(Guid currencyId, PaymentIntent paymentIntent) =>
        Where(configuration =>
            configuration.CurrencyId == currencyId &&
            configuration.PaymentIntent == paymentIntent &&
            configuration.IsEnabled);
}
