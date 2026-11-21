namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed record SubscriptionAnalyticsPlanResponse(
    Guid PlanId,
    string PlanCode,
    string PlanName,
    long SubscriptionCount,
    decimal Percentage);
