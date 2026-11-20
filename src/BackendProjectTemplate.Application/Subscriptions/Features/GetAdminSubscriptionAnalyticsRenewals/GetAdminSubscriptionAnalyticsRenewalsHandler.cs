using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsRenewals;

public sealed class GetAdminSubscriptionAnalyticsRenewalsHandler(
    ISubscriptionAnalyticsReadModelRepository repository,
    SubscriptionAnalyticsCache cache,
    TimeProvider timeProvider,
    IOptions<SubscriptionAnalyticsOptions> options)
{
    private readonly SubscriptionAnalyticsOptions _options = options.Value;

    public async Task<SubscriptionAnalyticsRenewalsResponse> HandleAsync(
        SubscriptionAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var (filter, _, _) = SubscriptionAnalyticsHandlerSupport.Resolve(query, timeProvider, _options);
        return await cache.GetOrCreateAsync("renewals", filter, null, _options.BreakdownTtl, async () =>
        {
            var row = await repository.GetRenewalsAsync(filter, cancellationToken);
            return new SubscriptionAnalyticsRenewalsResponse(
                row.SuccessfulRenewals,
                row.FailedRenewals,
                row.FailedPaymentAttempts,
                row.PendingRenewals,
                row.AwaitingReconciliationRenewals,
                AnalyticsMath.Percentage(
                    row.SuccessfulRenewals,
                    row.SuccessfulRenewals + row.FailedRenewals));
        }, cancellationToken);
    }
}
