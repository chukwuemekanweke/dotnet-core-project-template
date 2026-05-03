using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class SuccessfulPaymentConfirmedHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork) : BaseMessageHandler<SuccessfulPaymentConfirmed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SuccessfulPaymentConfirmed message, CancellationToken cancellationToken)
    {
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.SubscriberStarted,
            ObservabilityEventProperties.CreatePayment(
                CurrentActorAccessor,
                Observability.StepNames.SubscriberProcessing,
                Observability.Outcomes.Started,
                message.StakeholderId,
                paymentReference: message.MerchantReference,
                paymentIntent: message.PaymentIntent,
                amount: message.Amount,
                currencyId: message.CurrencyId,
                source: Observability.Sources.Subscriber));

        switch (message.PaymentIntent)
        {
            case PaymentIntent.WalletTopUp:
                await commandSender.SendAsync(
                    new CreditWalletCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        TenantId = message.TenantId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                break;

            case PaymentIntent.Subscription:
                await commandSender.SendAsync(
                    new ActivateSubscriptionCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        TenantId = message.TenantId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                break;

            default:
                CustomTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.SubscriberFailed,
                    ObservabilityEventProperties.CreatePayment(
                        CurrentActorAccessor,
                        Observability.StepNames.SubscriberProcessing,
                        Observability.Outcomes.Failure,
                        message.StakeholderId,
                        ObservabilityFailureReasons.UnsupportedPaymentIntent,
                        paymentReference: message.MerchantReference,
                        paymentIntent: message.PaymentIntent,
                        amount: message.Amount,
                        currencyId: message.CurrencyId,
                        source: Observability.Sources.Subscriber));
                throw new CannotProcessMessageNonTransientException(
                    $"Unsupported payment intent '{message.PaymentIntent}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.SubscriberSucceeded,
            ObservabilityEventProperties.CreatePayment(
                CurrentActorAccessor,
                Observability.StepNames.SubscriberProcessing,
                Observability.Outcomes.Success,
                message.StakeholderId,
                paymentReference: message.MerchantReference,
                paymentIntent: message.PaymentIntent,
                amount: message.Amount,
                currencyId: message.CurrencyId,
                source: Observability.Sources.Subscriber));
    }
}
