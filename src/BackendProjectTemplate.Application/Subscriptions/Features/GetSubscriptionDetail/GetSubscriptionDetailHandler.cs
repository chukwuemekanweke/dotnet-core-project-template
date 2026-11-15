using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed class GetSubscriptionDetailHandler(ISubscriptionAdministrationReadModelRepository readModelRepository)
{
    public const int DefaultHistoryLimit = 50;
    public const int MaxHistoryLimit = 200;

    public async Task<SubscriptionDetailResponse?> HandleAsync(GetSubscriptionDetailCommand command, CancellationToken cancellationToken)
    {
        var historyLimit = Math.Clamp(command.HistoryLimit, 1, MaxHistoryLimit);
        var detail = await readModelRepository.GetSubscriptionDetailAsync(command.SubscriptionId, historyLimit, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        return new SubscriptionDetailResponse(
            AdminSubscriptionResponseMapper.ToResponse(detail.Summary),
            detail.CancellationRequestedAtUtc,
            detail.CancellationReason,
            detail.CancelledAtUtc,
            detail.EndedAtUtc,
            detail.LastSuccessfulPaymentTransactionId,
            detail.Stakeholder is null
                ? null
                : new SubscriptionDetailStakeholderResponse(detail.Stakeholder.StakeholderId, detail.Stakeholder.FirstName, detail.Stakeholder.LastName),
            AdminSubscriptionResponseMapper.ToResponse(detail.Plan),
            detail.Purchase is null
                ? null
                : new SubscriptionDetailPurchaseResponse(
                    detail.Purchase.PurchaseId,
                    detail.Purchase.PaymentTransactionId,
                    detail.Purchase.ExpectedAmount,
                    detail.Purchase.ExpectedCurrencyId,
                    detail.Purchase.InitiatedByStakeholderId,
                    detail.Purchase.CreatedAtUtc,
                    detail.Purchase.FulfilledAtUtc,
                    detail.Purchase.FulfillmentBlockedAtUtc,
                    detail.Purchase.FulfillmentBlockedReason),
            detail.Periods.Select(period => new SubscriptionDetailPeriodResponse(
                period.PeriodId,
                period.PeriodStartUtc,
                period.PeriodEndUtc,
                period.PlanCode,
                period.NominalPrice,
                period.CurrencyId,
                period.BillingInterval.ToString(),
                period.ActivationSource.ToString(),
                period.PaymentTransactionId,
                period.OperationId)).ToArray(),
            detail.Payments.Select(payment => new SubscriptionDetailPaymentResponse(
                payment.PaymentTransactionId,
                payment.MerchantReference,
                payment.Amount,
                payment.CurrencyId,
                payment.CurrencyCode,
                payment.PaymentStatus.ToString(),
                payment.RenewalId,
                payment.CreatedAtUtc,
                payment.CompletedAtUtc,
                payment.FailedAtUtc)).ToArray(),
            detail.Renewals.Select(renewal => new SubscriptionDetailRenewalResponse(
                renewal.RenewalId,
                renewal.TargetPeriodStartUtc,
                renewal.TargetPeriodEndUtc,
                renewal.ExpectedPrice,
                renewal.CurrencyId,
                renewal.Status.ToString(),
                renewal.CreatedAtUtc,
                renewal.CompletedAtUtc,
                renewal.NextRetryAtUtc,
                renewal.Attempts.Select(attempt => new SubscriptionDetailRenewalAttemptResponse(
                    attempt.AttemptId,
                    attempt.AttemptNumber,
                    attempt.PaymentTransactionId,
                    attempt.Status.ToString(),
                    attempt.FailureCode,
                    attempt.FailureReason,
                    attempt.CreatedAtUtc,
                    attempt.CompletedAtUtc)).ToArray())).ToArray(),
            detail.StatusHistory.Select(history => new SubscriptionDetailStatusHistoryResponse(
                history.HistoryId,
                history.PreviousStatus?.ToString(),
                history.NewStatus.ToString(),
                history.Reason,
                history.Details,
                history.ActorId,
                history.OperationId,
                history.PaymentTransactionId,
                history.OccurredAtUtc)).ToArray(),
            detail.Allocations.Select(allocation => new SubscriptionDetailAllocationResponse(
                allocation.AllocationId,
                allocation.AllocationSource.ToString(),
                allocation.Reason,
                allocation.AllocatedByActorId,
                allocation.OperationId,
                allocation.PlanId,
                allocation.PlanCode,
                allocation.NominalPrice,
                allocation.CurrencyId,
                allocation.BillingInterval.ToString(),
                allocation.StartAtUtc,
                allocation.EndAtUtc,
                allocation.CreatedAtUtc)).ToArray(),
            historyLimit);
    }
}
