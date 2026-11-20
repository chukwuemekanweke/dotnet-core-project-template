using BackendProjectTemplate.Application.Subscriptions.Analytics;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class AdminSubscriptionAnalyticsValidator
    : AdminSubscriptionAnalyticsValidatorBase<AdminSubscriptionAnalyticsRequest>
{
    public AdminSubscriptionAnalyticsValidator(
        IOptions<SubscriptionAnalyticsOptions> options,
        TimeProvider timeProvider) : base(options.Value, timeProvider)
    {
    }
}
