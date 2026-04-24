using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class SuccessfulPaymentConfirmedHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICommandSender commandSender) : BaseMessageHandler<SuccessfulPaymentConfirmed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(SuccessfulPaymentConfirmed message, CancellationToken cancellationToken)
    {
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
                throw new CannotProcessMessageNonTransientException(
                    $"Unsupported payment intent '{message.PaymentIntent}'.");
        }
    }
}
