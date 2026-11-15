namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed record AdminSubscriptionCancellationRequest(Guid TenantId, string Reason);
