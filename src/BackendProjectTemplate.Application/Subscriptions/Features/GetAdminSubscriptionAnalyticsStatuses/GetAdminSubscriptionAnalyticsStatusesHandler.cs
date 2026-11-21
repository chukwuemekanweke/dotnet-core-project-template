using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsStatuses;

public sealed class GetAdminSubscriptionAnalyticsStatusesHandler(
    ISubscriptionAnalyticsReadModelRepository repository,
    SubscriptionAnalyticsCache cache,
    TimeProvider timeProvider,
    IOptions<SubscriptionAnalyticsOptions> options)
{
    private readonly SubscriptionAnalyticsOptions _options = options.Value;

    public async Task<SubscriptionAnalyticsStatusesResponse> HandleAsync(
        SubscriptionAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var (filter, _, _) = SubscriptionAnalyticsHandlerSupport.Resolve(query, timeProvider, _options);
        return await cache.GetOrCreateAsync("statuses", filter, null, _options.BreakdownTtl, async () =>
        {
            var rows = await repository.GetStatusesAsync(filter, cancellationToken);
            var total = rows.Sum(row => row.SubscriptionCount);
            return new SubscriptionAnalyticsStatusesResponse(rows.Select(row => new SubscriptionAnalyticsStatusResponse(
                row.Status.ToString(),
                row.SubscriptionCount,
                AnalyticsMath.Percentage(row.SubscriptionCount, total))).ToArray());
        }, cancellationToken);
    }
}
