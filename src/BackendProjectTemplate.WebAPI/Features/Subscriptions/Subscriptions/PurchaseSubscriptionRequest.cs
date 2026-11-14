namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed record PurchaseSubscriptionRequest(
    Guid PlanId,
    Guid PaymentProviderId,
    string BillingAccountOwnerType,
    bool AutoRenew = true);
