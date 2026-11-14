using BackendProjectTemplate.Contracts.Commands.Subscriptions;
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

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class RetrySubscriptionRenewalHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionRenewal> renewalRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<RecurringPaymentAuthorization> authorizationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    SubscriptionRenewalCharger renewalCharger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<RetrySubscriptionRenewalHandler> logger) : BaseMessageHandler<RetrySubscriptionRenewalCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(RetrySubscriptionRenewalCommand message, CancellationToken cancellationToken)
    {
        var renewal = await renewalRepository.FirstOrDefaultAsync(
            new SubscriptionRenewalByIdSpecification(message.RenewalId),
            cancellationToken);
        if (renewal is null ||
            renewal.TenantId != message.TenantId ||
            renewal.Status != SubscriptionRenewalStatus.RetryScheduled ||
            renewal.Attempts.Count != message.ExpectedAttemptNumber ||
            (renewal.NextRetryAtUtc.HasValue && Clock.GetUtcNow() < renewal.NextRetryAtUtc.Value))
        {
            return;
        }

        var previousAttempt = renewal.Attempts.OrderByDescending(attempt => attempt.AttemptNumber).First();
        var authorization = await authorizationRepository.GetByIdAsync(previousAttempt.RecurringAuthorizationId, cancellationToken);
        if (authorization is null || !authorization.IsUsableAt(Clock.GetUtcNow()) || authorization.CurrencyId != renewal.CurrencyId)
        {
            renewal.MarkAwaitingAuthorization();
            return;
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(renewal.SubscriptionId),
            cancellationToken);
        if (subscription is null || !subscription.AutoRenew || subscription.CancelAtPeriodEnd ||
            subscription.CurrentPeriodEndUtc != renewal.TargetPeriodStartUtc)
        {
            renewal.MarkCancelled(Clock.GetUtcNow());
            return;
        }

        var identitySource = subscription.LastSuccessfulPaymentTransactionId.HasValue
            ? await paymentTransactionRepository.GetByIdAsync(subscription.LastSuccessfulPaymentTransactionId.Value, cancellationToken)
            : null;
        if (identitySource is null)
        {
            throw new InvalidOperationException($"Subscription '{subscription.Id}' has no successful payment identity for renewal.");
        }

        await renewalCharger.ChargeAsync(renewal, subscription, authorization, identitySource, cancellationToken);
    }
}
