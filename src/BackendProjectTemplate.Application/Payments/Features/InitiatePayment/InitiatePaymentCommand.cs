using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Payments.Features.InitiatePayment;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record InitiatePaymentCommand(
    decimal Amount,
    Guid CurrencyId,
    PaymentIntent PaymentIntent,
    Guid PaymentProviderId,
    ActorContext ActorContext,
    TrustedSubscriptionPaymentContext? TrustedSubscriptionContext = null);

public sealed record TrustedSubscriptionPaymentContext(
    Guid BillingAccountId,
    bool SavePaymentMethod,
    DateTimeOffset? ConsentedAtUtc);
