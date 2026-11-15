namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record SubscriptionDetailPurchaseResponse(
    Guid PurchaseId,
    Guid PaymentTransactionId,
    decimal ExpectedAmount,
    Guid ExpectedCurrencyId,
    Guid InitiatedByStakeholderId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? FulfilledAtUtc,
    DateTimeOffset? FulfillmentBlockedAtUtc,
    string? FulfillmentBlockedReason);
