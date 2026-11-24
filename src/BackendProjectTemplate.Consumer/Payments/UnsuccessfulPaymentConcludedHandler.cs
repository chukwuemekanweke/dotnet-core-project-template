using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Payments;

/// <summary>
/// Releases a Pending subscription whose initial payment can never succeed, so the BillingAccount no longer
/// reports it as current and the customer can start a new purchase. Renewal payments have no purchase and are
/// handled by the renewal flow, so they are ignored here.
/// </summary>
public sealed class UnsuccessfulPaymentConcludedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<SubscriptionPurchase> purchaseRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<UnsuccessfulPaymentConcludedHandler> logger) : BaseMessageHandler<UnsuccessfulPaymentConcluded>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(UnsuccessfulPaymentConcluded message, CancellationToken cancellationToken)
    {
        if (message.PaymentIntent != PaymentIntent.Subscription)
        {
            return;
        }

        // Re-read the payment: only a terminal, non-successful payment may release the subscription.
        var payment = await paymentTransactionRepository.GetByIdAsync(message.PaymentTransactionId, cancellationToken);
        if (payment is null ||
            payment.TenantId != message.TenantId ||
            !payment.HasReachedTerminalState() ||
            payment.PaymentStatus == PaymentStatus.Succeeded)
        {
            return;
        }

        var purchase = await purchaseRepository.FirstOrDefaultAsync(
            new SubscriptionPurchaseByPaymentTransactionSpecification(payment.Id),
            cancellationToken);
        if (purchase is null)
        {
            return;
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(purchase.SubscriptionId),
            cancellationToken);
        if (subscription is null || subscription.TenantId != purchase.TenantId)
        {
            return;
        }

        if (!subscription.ExpireUnpaidPurchase(payment.Id, Clock.GetUtcNow(), message.MessageId))
        {
            return;
        }

        var history = subscription.StatusHistory.Single(item => item.OperationId == message.MessageId);
        await lifecyclePersistence.AddStatusHistoryAsync(history, cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.PurchaseExpiredUnpaid,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                message.StakeholderId,
                failureReason: payment.PaymentStatus.ToString(),
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.PurchaseId] = purchase.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString(),
                    [Observability.PropertyNames.Payments.PaymentReference] = payment.MerchantReference
                }));
    }
}
