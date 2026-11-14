using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Application.Payments.Features.InitiatePayment;

public sealed class InitiatePaymentHandler(PaymentInitiationService paymentInitiationService)
{
    public Task<InitiatePaymentResult> HandleAsync(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.PaymentIntent == PaymentIntent.Subscription)
        {
            throw new InvalidOperationException("Subscription payments must be initiated through the subscription purchase endpoint.");
        }

        return paymentInitiationService.InitiateAsync(command, null, cancellationToken);
    }
}
