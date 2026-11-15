using BackendProjectTemplate.Application.Subscriptions.Administration;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

/// <summary>
/// Administrative subscription detail. Financial history (<see cref="Purchase"/>, <see cref="Payments"/>,
/// <see cref="Renewals"/>) is kept separate from entitlement history (<see cref="Periods"/>,
/// <see cref="StatusHistory"/>, <see cref="Allocations"/>). History collections are bounded, newest first.
/// </summary>
public sealed record SubscriptionDetailResponse(
    AdminSubscriptionSummaryResponse Subscription,
    DateTimeOffset? CancellationRequestedAtUtc,
    string? CancellationReason,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? EndedAtUtc,
    Guid? LastSuccessfulPaymentTransactionId,
    SubscriptionDetailStakeholderResponse? Stakeholder,
    AdminSubscriptionPlanResponse Plan,
    SubscriptionDetailPurchaseResponse? Purchase,
    IReadOnlyList<SubscriptionDetailPeriodResponse> Periods,
    IReadOnlyList<SubscriptionDetailPaymentResponse> Payments,
    IReadOnlyList<SubscriptionDetailRenewalResponse> Renewals,
    IReadOnlyList<SubscriptionDetailStatusHistoryResponse> StatusHistory,
    IReadOnlyList<SubscriptionDetailAllocationResponse> Allocations,
    int HistoryLimit);
