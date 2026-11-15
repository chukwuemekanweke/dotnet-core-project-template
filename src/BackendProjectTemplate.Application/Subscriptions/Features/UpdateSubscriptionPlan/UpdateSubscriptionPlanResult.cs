using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanResult(
    UpdateSubscriptionPlanStatus Status,
    AdminSubscriptionPlanResponse? Plan,
    string? Error);
