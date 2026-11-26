namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

/// <summary>The optional reason is recorded in the administrative audit journal.</summary>
public sealed record RetryPurchaseFulfillmentRequest(Guid TenantId, string? Reason = null);
