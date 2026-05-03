using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class ActivateSubscriptionHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionActivation> subscriptionActivationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : BaseMessageHandler<ActivateSubscriptionCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(ActivateSubscriptionCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.ValueGrantFailed,
                ObservabilityEventProperties.CreatePayment(
                    CurrentActorAccessor,
                    Observability.StepNames.ValueGrant,
                    Observability.Outcomes.Failure,
                    failureReason: ObservabilityFailureReasons.StakeholderNotFound,
                    paymentReference: message.MerchantReference,
                    amount: message.Amount,
                    currencyId: message.CurrencyId,
                    source: Observability.Sources.Subscriber));
            throw new CannotProcessMessageNonTransientException("ActivateSubscriptionCommand must contain a valid stakeholder id.");
        }

        var existingActivation = await subscriptionActivationRepository.FirstOrDefaultAsync(
            new SubscriptionActivationByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingActivation is not null)
        {
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.ValueGrantFailed,
                ObservabilityEventProperties.CreatePayment(
                    CurrentActorAccessor,
                    Observability.StepNames.ValueGrant,
                    Observability.Outcomes.Duplicate,
                    message.StakeholderId,
                    ObservabilityFailureReasons.DuplicateProcessing,
                    paymentReference: message.MerchantReference,
                    amount: message.Amount,
                    currencyId: message.CurrencyId,
                    source: Observability.Sources.Subscriber,
                    isDuplicate: true));
            return;
        }

        await subscriptionActivationRepository.AddAsync(
            SubscriptionActivation.Create(
                message.PaymentTransactionId,
                message.StakeholderId.Value,
                message.TenantId,
                null,
                message.Amount,
                message.CurrencyId,
                timeProvider.GetUtcNow()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.ValueGranted,
            ObservabilityEventProperties.CreatePayment(
                CurrentActorAccessor,
                Observability.StepNames.ValueGrant,
                Observability.Outcomes.Success,
                message.StakeholderId,
                paymentReference: message.MerchantReference,
                amount: message.Amount,
                currencyId: message.CurrencyId,
                source: Observability.Sources.Subscriber));
    }
}
