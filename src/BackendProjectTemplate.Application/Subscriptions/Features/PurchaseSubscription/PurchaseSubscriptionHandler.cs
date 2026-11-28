using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public sealed class PurchaseSubscriptionHandler(
    IRepository<SubscriptionPlan> planRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionPurchase> purchaseRepository,
    IRepository<PaymentTransaction> paymentRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IBillingAccountResolver billingAccountResolver,
    PaymentInitiationService paymentInitiationService,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
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
        if (command.IdempotencyKey == Guid.Empty)
        {
            throw new ArgumentException("A non-empty idempotency key is required.", nameof(command));
        }

        var requestFingerprint = PurchaseSubscriptionRequestFingerprint.Compute(command);
        var plan = await planRepository.FirstOrDefaultAsync(
            new ActiveSubscriptionPlanByIdSpecification(command.PlanId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Subscription plan '{command.PlanId}' is not active.");
        if (!plan.Offers(command.BillingInterval))
        {
            throw new InvalidOperationException(
                $"Subscription plan '{command.PlanId}' is not offered with a {command.BillingInterval} billing interval.");
        }

        var price = plan.GetPrice(command.BillingInterval);
        if (price <= 0)
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

        var existingPurchase = await purchaseRepository.FirstOrDefaultAsync(
            new SubscriptionPurchaseByOperationIdSpecification(tenantId, stakeholderId, command.IdempotencyKey),
            cancellationToken);
        if (existingPurchase is not null)
        {
            return await CreateReplayResultAsync(existingPurchase, requestFingerprint, cancellationToken);
        }

        // One active entitlement per BillingAccount: do not take money for a purchase that could never be
        // fulfilled because the account already holds a current or scheduled entitlement. Fulfillment
        // re-checks this at payment confirmation, where the PostgreSQL exclusion constraint is authoritative.
        if (await subscriptionRepository.AnyAsync(
                new OverlappingEntitlementSpecification(tenantId, billingAccount.Id, timeProvider.GetUtcNow(), null, null),
                cancellationToken))
        {
            throw new SubscriptionEntitlementConflictException(
                "The billing account already holds or has reserved a subscription entitlement.");
        }

        SubscriptionAggregate? subscription = null;
        SubscriptionPurchase? purchase = null;
        InitiatePaymentResult paymentResult;
        try
        {
            paymentResult = await paymentInitiationService.InitiateAsync(
                new InitiatePaymentCommand(
                    price,
                    plan.CurrencyId,
                    PaymentIntent.Subscription,
                    command.PaymentProviderId,
                    command.ActorContext,
                    new TrustedSubscriptionPaymentContext(
                        billingAccount.Id,
                        command.AutoRenew,
                        command.AutoRenew ? timeProvider.GetUtcNow() : null)),
                async (paymentTransaction, token) =>
                {
                    var operationId = Guid.CreateVersion7();
                    subscription = SubscriptionAggregate.CreatePending(
                        tenantId,
                        billingAccount.Id,
                        plan.Id,
                        command.BillingInterval,
                        command.AutoRenew,
                        timeProvider.GetUtcNow(),
                        operationId,
                        stakeholderId.ToString());
                    purchase = SubscriptionPurchase.Create(
                        tenantId,
                        billingAccount.Id,
                        subscription.Id,
                        plan,
                        command.BillingInterval,
                        paymentTransaction.Id,
                        stakeholderId,
                        command.IdempotencyKey,
                        requestFingerprint);
                    await subscriptionRepository.AddAsync(subscription, token);
                    await purchaseRepository.AddAsync(purchase, token);
                    return purchase.Id;
                },
                cancellationToken);
        }
        catch (SubscriptionPurchaseOperationConflictException)
        {
            unitOfWork.DiscardChanges();
            existingPurchase = await purchaseRepository.FirstOrDefaultAsync(
                new SubscriptionPurchaseByOperationIdSpecification(tenantId, stakeholderId, command.IdempotencyKey),
                cancellationToken);
            if (existingPurchase is null)
            {
                throw;
            }

            return await CreateReplayResultAsync(existingPurchase, requestFingerprint, cancellationToken);
        }

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

    private async Task<PurchaseSubscriptionResult> CreateReplayResultAsync(
        SubscriptionPurchase purchase,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(purchase.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            throw new SubscriptionPurchaseIdempotencyKeyReusedException(
                "The idempotency key was already used for a different subscription purchase request.");
        }

        var payment = await paymentRepository.GetByIdAsync(purchase.PaymentTransactionId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment '{purchase.PaymentTransactionId}' was not found.");
        var provider = await paymentProviderRepository.GetByIdAsync(payment.PaymentProviderId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{payment.PaymentProviderId}' was not found.");
        return new PurchaseSubscriptionResult(
            purchase.SubscriptionId,
            purchase.Id,
            payment.MerchantReference,
            payment.PaymentStatus,
            provider.Id,
            provider.ProviderName,
            payment.ExpiresAtUtc,
            payment.PaymentMethodType,
            payment.ProviderPayloadMetadata);
    }
}
