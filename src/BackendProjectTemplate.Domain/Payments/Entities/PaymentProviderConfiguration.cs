using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class PaymentProviderConfiguration : Entity, IAggregateRoot
{
    private PaymentProviderConfiguration()
    {
    }

    private PaymentProviderConfiguration(
        Guid paymentProviderId,
        Guid currencyId,
        PaymentIntent paymentIntent,
        PaymentMethodType paymentMethodType,
        bool isEnabled)
    {
        PaymentProviderId = paymentProviderId;
        CurrencyId = currencyId;
        PaymentIntent = paymentIntent;
        PaymentMethodType = paymentMethodType;
        IsEnabled = isEnabled;
    }

    public Guid PaymentProviderId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public PaymentIntent PaymentIntent { get; private set; }
    public PaymentMethodType PaymentMethodType { get; private set; }
    public bool IsEnabled { get; private set; }

    public static PaymentProviderConfiguration Create(
        Guid paymentProviderId,
        Guid currencyId,
        PaymentIntent paymentIntent,
        PaymentMethodType paymentMethodType,
        bool isEnabled,
        DateTimeOffset utcNow) =>
        new(paymentProviderId, currencyId, paymentIntent, paymentMethodType, isEnabled);

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
