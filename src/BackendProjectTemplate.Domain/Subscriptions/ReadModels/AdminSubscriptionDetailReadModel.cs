namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record AdminSubscriptionDetailReadModel(
    AdminSubscriptionListItemReadModel Summary,
    DateTimeOffset? CancellationRequestedAtUtc,
    string? CancellationReason,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? EndedAtUtc,
    Guid? LastSuccessfulPaymentTransactionId,
    AdminSubscriptionStakeholderReadModel? Stakeholder,
    AdminSubscriptionPlanReadModel Plan,
    AdminSubscriptionPurchaseReadModel? Purchase,
    IReadOnlyList<AdminSubscriptionPeriodReadModel> Periods,
    IReadOnlyList<AdminSubscriptionPaymentReadModel> Payments,
    IReadOnlyList<AdminSubscriptionRenewalReadModel> Renewals,
    IReadOnlyList<AdminSubscriptionStatusHistoryReadModel> StatusHistory,
    IReadOnlyList<AdminSubscriptionAllocationReadModel> Allocations);
