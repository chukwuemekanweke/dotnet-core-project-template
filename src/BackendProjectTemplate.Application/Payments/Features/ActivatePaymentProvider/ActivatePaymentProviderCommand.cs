using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;

public sealed record ActivatePaymentProviderCommand(
    Guid PaymentProviderId,
    bool IsActive,
    ActorContext ActorContext,
    string? Reason = null);
