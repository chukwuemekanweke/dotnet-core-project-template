using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanResult(
    CreateSubscriptionPlanStatus Status,
    AdminSubscriptionPlanResponse? Plan,
    string? Error);
