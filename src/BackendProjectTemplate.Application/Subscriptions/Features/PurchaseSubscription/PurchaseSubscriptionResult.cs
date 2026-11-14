using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public sealed record PurchaseSubscriptionResult(
    Guid SubscriptionId,
    Guid PurchaseId,
    string MerchantReference,
    PaymentStatus PaymentStatus,
    Guid PaymentProviderId,
    string PaymentProviderName,
    DateTimeOffset? ExpiresAtUtc,
    PaymentMethodType PaymentMethodType,
    IReadOnlyDictionary<string, string> InstructionFields);
