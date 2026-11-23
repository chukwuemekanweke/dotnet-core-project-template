using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;

public sealed class GetSubscriptionPurchaseStatusHandler(ICustomerSubscriptionReadModelRepository readModelRepository)
{
    public async Task<GetSubscriptionPurchaseStatusResponse?> HandleAsync(
        GetSubscriptionPurchaseStatusCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required.");
        var purchase = await readModelRepository.GetPurchaseStatusAsync(
            new CustomerSubscriptionPurchaseStatusRequest(
                command.PurchaseId,
                tenantId,
                command.BillingAccountOwnerType,
                stakeholderId),
            cancellationToken);
        if (purchase is null)
        {
            return null;
        }

        var status = GetStatus(purchase);
        var shouldContinuePolling = status is "PurchaseInitiated" or "PaymentPending" or "PaymentSucceededFulfillmentPending";
        return new GetSubscriptionPurchaseStatusResponse(
            purchase.PurchaseId,
            purchase.SubscriptionId,
            purchase.MerchantReference,
            status,
            purchase.PaymentStatus.ToString(),
            purchase.SubscriptionStatus.ToString(),
            purchase.FulfillmentBlockedAtUtc.HasValue
                ? "Blocked"
                : purchase.FulfilledAtUtc.HasValue ? "Fulfilled" : "Pending",
            !shouldContinuePolling,
            shouldContinuePolling,
            purchase.Amount,
            purchase.CurrencyCode,
            purchase.PaymentProviderName,
            purchase.PaymentMethodType.ToString(),
            purchase.PaymentExpiresAtUtc,
            purchase.PaymentCompletedAtUtc,
            purchase.FulfilledAtUtc,
            purchase.FulfillmentBlockedAtUtc,
            purchase.FulfillmentBlockedReason,
            purchase.CurrentPeriodEndUtc);
    }

    private static string GetStatus(CustomerSubscriptionPurchaseStatusReadModel purchase)
    {
        if (purchase.FulfillmentBlockedAtUtc.HasValue)
        {
            return "FulfillmentBlocked";
        }

        return purchase.PaymentStatus switch
        {
            PaymentStatus.PendingInitiation => "PurchaseInitiated",
            PaymentStatus.Initiated => "PaymentPending",
            PaymentStatus.Failed => "PaymentFailed",
            PaymentStatus.Cancelled => "PaymentCancelled",
            PaymentStatus.Expired => "PaymentExpired",
            PaymentStatus.Underpaid => "PaymentUnderpaid",
            PaymentStatus.Succeeded when !purchase.FulfilledAtUtc.HasValue => "PaymentSucceededFulfillmentPending",
            PaymentStatus.Succeeded => purchase.SubscriptionStatus switch
            {
                SubscriptionStatus.Active => "SubscriptionActive",
                SubscriptionStatus.Cancelled => "SubscriptionCancelled",
                SubscriptionStatus.Expired => "SubscriptionExpired",
                SubscriptionStatus.Scheduled => "SubscriptionScheduled",
                _ => "SubscriptionPending"
            },
            _ => "PurchaseInitiated"
        };
    }
}
