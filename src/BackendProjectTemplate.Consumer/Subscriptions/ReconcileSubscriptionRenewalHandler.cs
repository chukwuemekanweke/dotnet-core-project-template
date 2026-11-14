using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class ReconcileSubscriptionRenewalHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionRenewal> renewalRepository,
    IRepository<RecurringPaymentAuthorization> authorizationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    SubscriptionRenewalCharger renewalCharger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ReconcileSubscriptionRenewalHandler> logger) : BaseMessageHandler<ReconcileSubscriptionRenewalCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(ReconcileSubscriptionRenewalCommand message, CancellationToken cancellationToken)
    {
        var renewal = await renewalRepository.FirstOrDefaultAsync(
            new SubscriptionRenewalByIdSpecification(message.RenewalId),
            cancellationToken);
        var attempt = renewal?.Attempts.SingleOrDefault(candidate => candidate.Id == message.RenewalAttemptId);
        if (renewal is null || attempt is null || renewal.TenantId != message.TenantId || !attempt.PaymentTransactionId.HasValue)
        {
            return;
        }

        var payment = await paymentTransactionRepository.GetByIdAsync(attempt.PaymentTransactionId.Value, cancellationToken);
        var authorization = await authorizationRepository.GetByIdAsync(attempt.RecurringAuthorizationId, cancellationToken);
        if (payment is null || authorization is null)
        {
            throw new InvalidOperationException($"Renewal attempt '{attempt.Id}' is missing its payment correlation.");
        }

        await renewalCharger.ReconcileAsync(renewal, attempt, payment, authorization, cancellationToken);
    }
}