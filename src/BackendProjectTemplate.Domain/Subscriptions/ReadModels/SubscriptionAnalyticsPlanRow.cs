namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsPlanRow(
    Guid PlanId,
    string PlanCode,
    string PlanName,
    long SubscriptionCount);
