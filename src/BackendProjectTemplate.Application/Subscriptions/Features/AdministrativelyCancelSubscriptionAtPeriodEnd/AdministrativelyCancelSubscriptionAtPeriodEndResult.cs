using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;

public sealed record AdministrativelyCancelSubscriptionAtPeriodEndResult(
    AdministrativelyCancelSubscriptionAtPeriodEndStatus Status,
    AdminSubscriptionLifecycleResponse? Subscription,
    string? Error);
