namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

/// <summary>The optional reason is recorded in the administrative audit journal.</summary>
public sealed record SetSubscriptionPlanActivationRequest(bool IsActive, string? Reason = null);
