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
    IRepository<SubscriptionActivation> subscriptionActivationRepository,
    ISubscriptionLifecyclePersistence lifecyclePersistence,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ActivateSubscriptionHandler> logger) : BaseMessageHandler<ActivateSubscriptionCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
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
            cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Subscription purchase for payment '{message.PaymentTransactionId}' was not found.");

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
}
