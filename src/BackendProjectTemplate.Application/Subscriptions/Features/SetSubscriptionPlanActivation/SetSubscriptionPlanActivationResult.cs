using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;

public sealed record SetSubscriptionPlanActivationResult(
    SetSubscriptionPlanActivationStatus Status,
    AdminSubscriptionPlanResponse? Plan,
    string? Error);
