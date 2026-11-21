using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsPlans;

public sealed class GetAdminSubscriptionAnalyticsPlansHandler(
    ISubscriptionAnalyticsReadModelRepository repository,
    SubscriptionAnalyticsCache cache,
    TimeProvider timeProvider,
    IOptions<SubscriptionAnalyticsOptions> options)
{
    private readonly SubscriptionAnalyticsOptions _options = options.Value;

    public async Task<SubscriptionAnalyticsPlansResponse> HandleAsync(
        SubscriptionAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var (filter, _, _) = SubscriptionAnalyticsHandlerSupport.Resolve(query, timeProvider, _options);
        return await cache.GetOrCreateAsync("plans", filter, null, _options.BreakdownTtl, async () =>
        {
            var rows = await repository.GetPlansAsync(filter, cancellationToken);
            var total = rows.Sum(row => row.SubscriptionCount);
            return new SubscriptionAnalyticsPlansResponse(rows.Select(row => new SubscriptionAnalyticsPlanResponse(
                row.PlanId,
                row.PlanCode,
                row.PlanName,
                row.SubscriptionCount,
                AnalyticsMath.Percentage(row.SubscriptionCount, total))).ToArray());
        }, cancellationToken);
    }
}
