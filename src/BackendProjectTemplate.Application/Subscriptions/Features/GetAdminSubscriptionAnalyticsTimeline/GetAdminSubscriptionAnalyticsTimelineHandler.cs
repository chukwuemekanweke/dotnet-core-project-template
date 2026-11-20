using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsTimeline;

public sealed class GetAdminSubscriptionAnalyticsTimelineHandler(
    ISubscriptionAnalyticsReadModelRepository repository,
    SubscriptionAnalyticsCache cache,
    TimeProvider timeProvider,
    IOptions<SubscriptionAnalyticsOptions> options)
{
    private readonly SubscriptionAnalyticsOptions _options = options.Value;

    public async Task<SubscriptionAnalyticsTimelineResponse> HandleAsync(
        SubscriptionAnalyticsQuery query,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var (filter, _, to) = SubscriptionAnalyticsHandlerSupport.Resolve(query, timeProvider, _options);
        return await cache.GetOrCreateAsync("timeline", filter, interval, _options.TimelineTtl, async () =>
        {
            var rows = await repository.GetTimelineAsync(filter, interval, cancellationToken);
            return new SubscriptionAnalyticsTimelineResponse(
                interval.ToString(),
                rows.Select(row => new SubscriptionAnalyticsTimelineBucketResponse(
                    row.PeriodStart,
                    AnalyticsCalendar.GetPeriodEnd(row.PeriodStart, to, interval),
                    row.ActiveSubscriptions,
                    row.NewSubscriptions,
                    row.ChurnedSubscriptions,
                    row.SuccessfulRenewals,
                    row.FailedRenewals,
                    SubscriptionAnalyticsHandlerSupport.MapRevenues(row.Revenues))).ToArray());
        }, cancellationToken);
    }
}
