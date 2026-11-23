namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record CustomerBillingHistoryPage(
    IReadOnlyList<CustomerBillingHistoryItemReadModel> Items,
    int Page,
    int PageSize,
    int TotalCount);
