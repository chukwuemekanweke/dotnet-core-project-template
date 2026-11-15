namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionListPage(
    IReadOnlyList<AdminSubscriptionListItemReadModel> Items,
    bool HasMore);
