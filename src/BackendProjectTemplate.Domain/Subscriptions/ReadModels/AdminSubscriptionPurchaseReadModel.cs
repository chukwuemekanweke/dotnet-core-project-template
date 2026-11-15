namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionPurchaseReadModel(
    Guid PurchaseId,
    Guid PaymentTransactionId,
    decimal ExpectedAmount,
    Guid ExpectedCurrencyId,
    Guid InitiatedByStakeholderId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? FulfilledAtUtc,
    DateTimeOffset? FulfillmentBlockedAtUtc,
    string? FulfillmentBlockedReason);
