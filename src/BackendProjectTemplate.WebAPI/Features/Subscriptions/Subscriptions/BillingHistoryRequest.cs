namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed record BillingHistoryRequest(
    string BillingAccountOwnerType,
    int Page = 1,
    int PageSize = 20);
