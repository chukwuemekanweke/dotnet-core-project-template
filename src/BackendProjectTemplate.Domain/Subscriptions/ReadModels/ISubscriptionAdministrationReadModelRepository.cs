namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public interface ISubscriptionAdministrationReadModelRepository
{
    Task<AdminSubscriptionListPage> ListSubscriptionsAsync(
        AdminSubscriptionListRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the subscription with financial history (payments, renewals and attempts) kept separate from
    /// entitlement history (status transitions and allocations). Each history collection is bounded to the
    /// most recent <paramref name="historyLimit"/> entries.
    /// </summary>
    Task<AdminSubscriptionDetailReadModel?> GetSubscriptionDetailAsync(
        Guid subscriptionId,
        int historyLimit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminSubscriptionPlanReadModel>> ListPlansAsync(
        bool? isActive,
        CancellationToken cancellationToken);
}
