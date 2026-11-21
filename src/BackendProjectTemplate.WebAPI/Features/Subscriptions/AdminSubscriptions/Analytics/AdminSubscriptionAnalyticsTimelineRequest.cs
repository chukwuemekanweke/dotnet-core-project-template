namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class AdminSubscriptionAnalyticsTimelineRequest : AdminSubscriptionAnalyticsRequest
{
    public string Interval { get; init; } = "Day";
}
