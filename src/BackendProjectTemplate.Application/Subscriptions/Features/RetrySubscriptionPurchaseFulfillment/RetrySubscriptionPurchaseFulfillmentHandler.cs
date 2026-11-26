using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;

/// <summary>
/// Compensating path for a verified payment whose fulfillment was blocked (for example by a conflicting
/// entitlement). After the administrator resolves the conflict, this re-enqueues the standard
/// <see cref="ActivateSubscriptionCommand"/> through the outbox; the consumer re-validates ownership, money and
/// the single-entitlement rule and either fulfills or blocks again. Refunds remain outside this domain.
/// </summary>
public sealed class RetrySubscriptionPurchaseFulfillmentHandler(
    IRepository<SubscriptionPurchase> purchaseRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    ICommandSender commandSender,
    IAdministrativeAuditRecorder auditRecorder,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RetrySubscriptionPurchaseFulfillmentResult> HandleAsync(
        RetrySubscriptionPurchaseFulfillmentCommand command,
        CancellationToken cancellationToken)
    {
        var purchase = await purchaseRepository.FirstOrDefaultAsync(
            new SubscriptionPurchaseBySubscriptionIdSpecification(command.SubscriptionId),
            cancellationToken);
        if (purchase is null || purchase.TenantId != command.TenantId)
        {
            return new RetrySubscriptionPurchaseFulfillmentResult(
                RetrySubscriptionPurchaseFulfillmentStatus.NotFound,
                null,
                null,
                $"No purchase was found for subscription '{command.SubscriptionId}'.");
        }

        if (!purchase.IsFulfillmentBlocked)
        {
            return new RetrySubscriptionPurchaseFulfillmentResult(
                RetrySubscriptionPurchaseFulfillmentStatus.NotBlocked,
                purchase.Id,
                purchase.PaymentTransactionId,
                $"Purchase '{purchase.Id}' is not awaiting fulfillment reconciliation.");
        }

        var payment = await paymentTransactionRepository.GetByIdAsync(purchase.PaymentTransactionId, cancellationToken);
        if (payment is null || payment.PaymentStatus != PaymentStatus.Succeeded)
        {
            return new RetrySubscriptionPurchaseFulfillmentResult(
                RetrySubscriptionPurchaseFulfillmentStatus.PaymentNotSucceeded,
                purchase.Id,
                purchase.PaymentTransactionId,
                $"Payment '{purchase.PaymentTransactionId}' is not a confirmed payment.");
        }

        await commandSender.SendAsync(
            new ActivateSubscriptionCommand(payment.Id, payment.MerchantReference, payment.Amount, payment.CurrencyId)
            {
                TenantId = payment.TenantId,
                StakeholderId = payment.StakeholderId
            },
            cancellationToken);
        await auditRecorder.RecordAsync(
            new AdministrativeAuditEntry(
                command.ActorContext,
                AdministrativeAuditActions.Subscriptions.PurchaseFulfillmentRetryRequested,
                AdministrativeAuditTargetTypes.Subscription,
                purchase.SubscriptionId,
                null,
                purchase.TenantId,
                command.Reason,
                null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.PurchaseFulfillmentRetryRequested,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                command.ActorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = purchase.SubscriptionId.ToString(),
                    [Observability.PropertyNames.Subscriptions.PurchaseId] = purchase.Id.ToString()
                }));

        return new RetrySubscriptionPurchaseFulfillmentResult(
            RetrySubscriptionPurchaseFulfillmentStatus.Requested,
            purchase.Id,
            payment.Id,
            null);
    }
}
