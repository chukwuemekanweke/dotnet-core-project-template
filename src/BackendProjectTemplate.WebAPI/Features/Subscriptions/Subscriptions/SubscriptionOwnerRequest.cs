namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed record SubscriptionOwnerRequest(string BillingAccountOwnerType = "Stakeholder");
