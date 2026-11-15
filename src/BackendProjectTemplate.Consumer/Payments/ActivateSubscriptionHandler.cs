using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class ActivateSubscriptionHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<SubscriptionPurchase> purchaseRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionRenewal> renewalRepository,
    IRepository<SubscriptionActivation> subscriptionActivationRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ActivateSubscriptionHandler> logger) : BaseMessageHandler<ActivateSubscriptionCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    private readonly ILogger<ActivateSubscriptionHandler> _logger = logger;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(ActivateSubscriptionCommand message, CancellationToken cancellationToken)
    {
        var existingActivation = await subscriptionActivationRepository.FirstOrDefaultAsync(
            new SubscriptionActivationByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingActivation is not null)
        {
            return;
        }

        var payment = await paymentTransactionRepository.GetByIdAsync(message.PaymentTransactionId, cancellationToken);
        if (payment is null ||
            payment.PaymentIntent != PaymentIntent.Subscription ||
            payment.PaymentStatus != PaymentStatus.Succeeded)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Payment transaction '{message.PaymentTransactionId}' is not a confirmed subscription payment.");
        }

        if (payment.TenantId != message.TenantId ||
            payment.Amount != message.Amount ||
            payment.CurrencyId != message.CurrencyId ||
            !string.Equals(payment.MerchantReference, message.MerchantReference, StringComparison.Ordinal))
        {
            throw new CannotProcessMessageNonTransientException(
                $"ActivateSubscriptionCommand does not match payment transaction '{message.PaymentTransactionId}'.");
        }

        var purchase = await purchaseRepository.FirstOrDefaultAsync(
            new SubscriptionPurchaseByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (purchase is null)
        {
            await FulfillRenewalAsync(payment, message, cancellationToken);
            return;
        }

        try
        {
            purchase.ValidatePayment(message.TenantId, message.Amount, message.CurrencyId);
        }
        catch (Exception exception)
        {
            throw new CannotProcessMessageNonTransientException(exception.Message);
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(purchase.SubscriptionId),
            cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Subscription '{purchase.SubscriptionId}' was not found.");

        if (subscription.TenantId != purchase.TenantId || subscription.BillingAccountId != purchase.BillingAccountId)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Subscription '{subscription.Id}' does not match purchase ownership.");
        }

        var occurredAtUtc = Clock.GetUtcNow();
        if (subscription.FindPeriodForPayment(payment.Id) is null)
        {
            // A verified payment must never bypass the one-entitlement-per-BillingAccount rule. If it cannot be
            // fulfilled, keep the financial record, leave the purchase unfulfilled and flag it for an explicit
            // administrative retry or refund instead of throwing (which would only redeliver forever).
            if (subscription.Status != SubscriptionStatus.Pending)
            {
                BlockPurchase(purchase, subscription, message, SubscriptionFulfillmentBlockReasons.SubscriptionNotPending, occurredAtUtc);
                return;
            }

            var (entitlementStartUtc, entitlementEndUtc) = subscription.ProjectPaidEntitlement(purchase, occurredAtUtc);
            if (await subscriptionRepository.AnyAsync(
                    new OverlappingEntitlementSpecification(
                        subscription.TenantId,
                        subscription.BillingAccountId,
                        entitlementStartUtc,
                        entitlementEndUtc,
                        subscription.Id),
                    cancellationToken))
            {
                BlockPurchase(purchase, subscription, message, SubscriptionFulfillmentBlockReasons.EntitlementConflict, occurredAtUtc);
                return;
            }
        }

        var period = subscription.ActivateFromPayment(
            purchase,
            payment.Id,
            occurredAtUtc,
            purchase.Id);
        var statusHistory = subscription.StatusHistory.Single(history => history.OperationId == purchase.Id);
        await lifecyclePersistence.AddPeriodAsync(period, cancellationToken);
        await lifecyclePersistence.AddStatusHistoryAsync(statusHistory, cancellationToken);
        purchase.MarkFulfilled(occurredAtUtc);

        await subscriptionActivationRepository.AddAsync(
            SubscriptionActivation.Create(
                payment.Id,
                purchase.BillingAccountId,
                subscription,
                period,
                purchase.InitiatedByStakeholderId,
                purchase.TenantId,
                subscription.Id.ToString(),
                purchase.ExpectedAmount,
                purchase.ExpectedCurrencyId,
                occurredAtUtc),
            cancellationToken);

        if (subscription.AutoRenew && subscription.NextBillingAtUtc.HasValue)
        {
            await commandSender.ScheduleSendAsync(
                new RenewSubscriptionCommand(subscription.Id, period.PeriodEndUtc),
                subscription.NextBillingAtUtc.Value,
                cancellationToken);
        }
        else if (!subscription.AutoRenew && subscription.CurrentPeriodEndUtc.HasValue)
        {
            await commandSender.ScheduleSendAsync(
                new ExpireSubscriptionCommand(subscription.Id, subscription.CurrentPeriodEndUtc.Value)
                {
                    TenantId = subscription.TenantId
                },
                subscription.CurrentPeriodEndUtc.Value,
                cancellationToken);
        }

        // The application check above cannot see a concurrent, uncommitted grant. PostgreSQL's exclusion
        // constraint is authoritative: if a competing entitlement wins, discard the activation and persist the
        // blocked purchase in this same delivery instead of depending on broker redelivery.
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SubscriptionEntitlementConflictException)
        {
            _unitOfWork.DiscardChanges();
            var blockedPurchase = await purchaseRepository.FirstOrDefaultAsync(
                new SubscriptionPurchaseByPaymentTransactionSpecification(message.PaymentTransactionId),
                cancellationToken)
                ?? throw new CannotProcessMessageNonTransientException(
                    $"Purchase for payment '{message.PaymentTransactionId}' could not be reloaded after an entitlement conflict.");
            BlockPurchase(
                blockedPurchase,
                subscription,
                message,
                SubscriptionFulfillmentBlockReasons.EntitlementConflict,
                Clock.GetUtcNow());
            return;
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.SubscriptionActivated,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                    [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                }));
    }

    private void BlockPurchase(
        SubscriptionPurchase purchase,
        SubscriptionAggregate subscription,
        ActivateSubscriptionCommand message,
        string reason,
        DateTimeOffset blockedAtUtc)
    {
        purchase.MarkFulfillmentBlocked(reason, blockedAtUtc);
        _logger.LogError(
            "Payment {PaymentTransactionId} for purchase {PurchaseId} was verified but subscription {SubscriptionId} cannot be activated ({Reason}); the purchase awaits administrative reconciliation.",
            message.PaymentTransactionId,
            purchase.Id,
            subscription.Id,
            reason);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.PurchaseFulfillmentBlocked,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                failureReason: reason,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.PurchaseId] = purchase.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString(),
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference
                }));
    }

    private async Task FulfillRenewalAsync(
        PaymentTransaction payment,
        ActivateSubscriptionCommand message,
        CancellationToken cancellationToken)
    {
        var renewal = await renewalRepository.FirstOrDefaultAsync(
            new SubscriptionRenewalByPaymentTransactionSpecification(payment.Id),
            cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"No subscription purchase or renewal was found for payment '{payment.Id}'.");
        if (renewal.Status is SubscriptionRenewalStatus.Fulfilled or SubscriptionRenewalStatus.FulfillmentBlocked)
        {
            return;
        }

        var attempt = renewal.Attempts.Single(candidate => candidate.PaymentTransactionId == payment.Id);
        if (renewal.TenantId != message.TenantId ||
            renewal.ExpectedPrice != payment.Amount ||
            renewal.CurrencyId != payment.CurrencyId)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Renewal '{renewal.Id}' does not match payment '{payment.Id}'.");
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(renewal.SubscriptionId),
            cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Subscription '{renewal.SubscriptionId}' was not found.");
        if (subscription.TenantId != renewal.TenantId || subscription.BillingAccountId != renewal.BillingAccountId)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Renewal '{renewal.Id}' does not match subscription ownership.");
        }

        var occurredAtUtc = Clock.GetUtcNow();
        if (subscription.FindPeriodForPayment(payment.Id) is null &&
            (subscription.Status != SubscriptionStatus.Active || subscription.CurrentPeriodEndUtc != renewal.TargetPeriodStartUtc))
        {
            // The charge succeeded but the subscription was cancelled (or otherwise moved on) while it was in
            // flight. Never resurrect entitlement: record the collected payment against the attempt and flag the
            // renewal for financial reconciliation.
            attempt.MarkSucceeded(occurredAtUtc);
            renewal.MarkFulfillmentBlocked(occurredAtUtc);
            _logger.LogError(
                "Renewal {RenewalId} payment {PaymentTransactionId} succeeded but subscription {SubscriptionId} in status {Status} cannot be extended; reconciliation required.",
                renewal.Id,
                payment.Id,
                subscription.Id,
                subscription.Status);
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Subscriptions.RenewalFulfillmentBlocked,
                ObservabilityEventProperties.Create(
                    CurrentActorAccessor,
                    message.StakeholderId,
                    failureReason: SubscriptionFulfillmentBlockReasons.SubscriptionNotActive,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                        [Observability.PropertyNames.Subscriptions.RenewalId] = renewal.Id.ToString()
                    }));
            return;
        }

        var period = subscription.RenewFromPayment(renewal, payment.Id, occurredAtUtc, renewal.Id);
        var statusHistory = subscription.StatusHistory.Single(history => history.OperationId == renewal.Id);
        await lifecyclePersistence.AddPeriodAsync(period, cancellationToken);
        await lifecyclePersistence.AddStatusHistoryAsync(statusHistory, cancellationToken);
        attempt.MarkSucceeded(occurredAtUtc);
        renewal.MarkFulfilled(occurredAtUtc);

        if (subscription.AutoRenew && !subscription.CancelAtPeriodEnd && subscription.NextBillingAtUtc.HasValue)
        {
            await commandSender.ScheduleSendAsync(
                new RenewSubscriptionCommand(subscription.Id, period.PeriodEndUtc)
                {
                    TenantId = subscription.TenantId,
                    StakeholderId = payment.StakeholderId
                },
                subscription.NextBillingAtUtc.Value,
                cancellationToken);
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.SubscriptionActivated,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                    [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString(),
                    [Observability.PropertyNames.Subscriptions.RenewalId] = renewal.Id.ToString()
                }));
    }
}
