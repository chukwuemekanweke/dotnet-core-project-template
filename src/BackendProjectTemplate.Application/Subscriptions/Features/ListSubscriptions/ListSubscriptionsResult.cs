using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;

public sealed record ListSubscriptionsResult(
    IReadOnlyList<AdminSubscriptionSummaryResponse> Items,
    string? NextCursor);
