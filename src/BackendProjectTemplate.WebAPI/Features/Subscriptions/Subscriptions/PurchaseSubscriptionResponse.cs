namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed record PurchaseSubscriptionResponse(
    Guid SubscriptionId,
    Guid PurchaseId,
    string MerchantReference,
    string PaymentStatus,
    Guid PaymentProviderId,
    string PaymentProviderName,
    DateTimeOffset? ExpiresAtUtc,
    string PaymentMethodType,
    IReadOnlyDictionary<string, string> InstructionFields);
