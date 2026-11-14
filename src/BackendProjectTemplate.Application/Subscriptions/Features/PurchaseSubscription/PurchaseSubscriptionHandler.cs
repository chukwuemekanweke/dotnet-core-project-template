using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public sealed class PurchaseSubscriptionHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionPurchase> purchaseRepository,
    IBillingAccountResolver billingAccountResolver,
    PaymentInitiationService paymentInitiationService,
    ICustomTelemetryContext customTelemetryContext,
    TimeProvider timeProvider)
{
    public async Task<PurchaseSubscriptionResult> HandleAsync(
        PurchaseSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required to purchase a subscription.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to purchase a subscription.");
        var plan = await planRepository.FirstOrDefaultAsync(
            new ActiveSubscriptionPlanByIdSpecification(command.PlanId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan '{command.PlanId}' is not active.");
        if (plan.Price <= 0)
        {
            throw new InvalidOperationException("Free plans do not require payment initiation.");
        }

        Guid? ownerStakeholderId = command.BillingAccountOwnerType == BillingAccountOwnerType.Stakeholder
            ? stakeholderId
            : null;
        var billingAccount = await billingAccountResolver.ResolveOrCreateAsync(
            tenantId,
            command.BillingAccountOwnerType,
            ownerStakeholderId,
            cancellationToken);

        SubscriptionAggregate? subscription = null;
        SubscriptionPurchase? purchase = null;
        var paymentResult = await paymentInitiationService.InitiateAsync(
            new InitiatePaymentCommand(
                plan.Price,
                plan.CurrencyId,
                PaymentIntent.Subscription,
                command.PaymentProviderId,
                command.ActorContext),
            async (paymentTransaction, token) =>
            {
                var operationId = Guid.CreateVersion7();
                subscription = SubscriptionAggregate.CreatePending(
                    tenantId,
                    billingAccount.Id,
                    plan.Id,
                    command.AutoRenew,
                    timeProvider.GetUtcNow(),
                    operationId,
                    stakeholderId.ToString());
                purchase = SubscriptionPurchase.Create(
                    tenantId,
                    billingAccount.Id,
                    subscription.Id,
                    plan,
                    paymentTransaction.Id,
                    stakeholderId);
                await subscriptionRepository.AddAsync(subscription, token);
                await purchaseRepository.AddAsync(purchase, token);
            },
            cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.PurchaseInitiated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription!.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.PurchaseId] = purchase!.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.PlanId] = plan.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = billingAccount.Id.ToString()
                }));

        return new PurchaseSubscriptionResult(
            subscription!.Id,
            purchase!.Id,
            paymentResult.MerchantReference,
            paymentResult.PaymentStatus,
            paymentResult.PaymentProviderId,
            paymentResult.PaymentProviderName,
            paymentResult.ExpiresAtUtc,
            paymentResult.PaymentMethodType,
            paymentResult.InstructionFields);
    }
}
