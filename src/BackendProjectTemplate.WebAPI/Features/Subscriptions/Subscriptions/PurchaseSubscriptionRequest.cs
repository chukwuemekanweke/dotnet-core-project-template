namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed record PurchaseSubscriptionRequest(
    Guid PlanId,
    string BillingInterval,
    Guid PaymentProviderId,
    string BillingAccountOwnerType,
    bool AutoRenew = true);
