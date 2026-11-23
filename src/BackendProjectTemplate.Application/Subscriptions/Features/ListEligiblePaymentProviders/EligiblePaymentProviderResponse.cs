namespace BackendProjectTemplate.Application.Subscriptions.Features.ListEligiblePaymentProviders;

public sealed record EligiblePaymentProviderResponse(
    Guid Id,
    string Name,
    string PaymentMethodType,
    bool SupportsRecurringPayments);
