using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsSummary;

public sealed class GetAdminSubscriptionAnalyticsSummaryHandler(
    ISubscriptionAnalyticsReadModelRepository repository,
    SubscriptionAnalyticsCache cache,
    TimeProvider timeProvider,
    IOptions<SubscriptionAnalyticsOptions> options)
{
    private readonly SubscriptionAnalyticsOptions _options = options.Value;

    public async Task<SubscriptionAnalyticsSummaryResponse> HandleAsync(
        SubscriptionAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var (filter, from, to) = SubscriptionAnalyticsHandlerSupport.Resolve(query, timeProvider, _options);
        return await cache.GetOrCreateAsync("summary", filter, null, _options.SummaryTtl, async () =>
        {
            var row = await repository.GetSummaryAsync(filter, cancellationToken);
            return new SubscriptionAnalyticsSummaryResponse(
                from,
                to,
                filter.SnapshotUtc,
                row.ActiveSubscriptions,
                row.NewSubscriptions,
                row.ChurnedSubscriptions,
                row.PastDueSubscriptions,
                row.CancelAtPeriodEndSubscriptions,
                row.SuccessfulRenewals,
                row.FailedRenewals,
                AnalyticsMath.Percentage(
                    row.SuccessfulRenewals,
                    row.SuccessfulRenewals + row.FailedRenewals),
                SubscriptionAnalyticsHandlerSupport.MapRevenues(row.Revenues));
        }, cancellationToken);
    }
}
