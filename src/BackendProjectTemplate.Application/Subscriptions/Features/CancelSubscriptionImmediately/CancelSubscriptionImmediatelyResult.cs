using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;

public sealed record CancelSubscriptionImmediatelyResult(
    CancelSubscriptionImmediatelyStatus Status,
    AdminSubscriptionLifecycleResponse? Subscription,
    string? Error);
