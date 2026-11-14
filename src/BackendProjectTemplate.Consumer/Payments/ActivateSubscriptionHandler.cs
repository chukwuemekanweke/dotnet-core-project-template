using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Exceptions;
using BackendProjectTemplate.Domain.Payments.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Payments;

public sealed class ActivateSubscriptionHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionActivation> subscriptionActivationRepository,
    IBillingAccountResolver billingAccountResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ActivateSubscriptionHandler> logger) : BaseMessageHandler<ActivateSubscriptionCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
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

        BillingAccount billingAccount;
        try
        {
            billingAccount = await billingAccountResolver.ResolveOrCreateAsync(
                message.TenantId,
                BillingAccountOwnerType.Stakeholder,
                message.StakeholderId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is BillingAccountOwnerNotFoundException or BillingAccountResolutionException or ArgumentException)
        {
            throw new CannotProcessMessageNonTransientException(exception.Message);
        }

        await subscriptionActivationRepository.AddAsync(
            SubscriptionActivation.Create(
                message.PaymentTransactionId,
                billingAccount.Id,
                message.StakeholderId.Value,
                message.TenantId,
                null,
                message.Amount,
                message.CurrencyId,
                Clock.GetUtcNow()),
            cancellationToken);

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
