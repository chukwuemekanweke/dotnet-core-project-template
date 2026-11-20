using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public interface ISubscriptionAnalyticsReadModelRepository
{
    Task<SubscriptionAnalyticsSummaryReadModel> GetSummaryAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionAnalyticsTimelineRow>> GetTimelineAsync(
        SubscriptionAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken);

    Task<SubscriptionAnalyticsRenewalsReadModel> GetRenewalsAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionAnalyticsPlanRow>> GetPlansAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionAnalyticsStatusRow>> GetStatusesAsync(
        SubscriptionAnalyticsFilter filter,
        CancellationToken cancellationToken);
}
