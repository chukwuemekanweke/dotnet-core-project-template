namespace BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;

public sealed record GetBillingHistoryResponse(
    IReadOnlyList<BillingHistoryItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
