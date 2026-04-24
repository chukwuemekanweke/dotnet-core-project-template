using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Payments.Features.InitiatePayment;

public sealed record InitiatePaymentCommand(
    decimal Amount,
    Guid CurrencyId,
    PaymentIntent PaymentIntent,
    Guid PaymentProviderId);
