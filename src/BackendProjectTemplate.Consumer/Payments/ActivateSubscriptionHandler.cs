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
            throw new CannotProcessMessageNonTransientException("ActivateSubscriptionCommand must contain a valid stakeholder id.");
        }

        var existingActivation = await subscriptionActivationRepository.FirstOrDefaultAsync(
            new SubscriptionActivationByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingActivation is not null)
        {
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
