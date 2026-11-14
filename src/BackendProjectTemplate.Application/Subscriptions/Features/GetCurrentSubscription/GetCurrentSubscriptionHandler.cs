using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;

public sealed class GetCurrentSubscriptionHandler(
    IReadRepository<BillingAccount> billingAccountRepository,
    IReadRepository<SubscriptionAggregate> subscriptionRepository,
    IReadRepository<SubscriptionPlan> planRepository)
{
    public async Task<GetCurrentSubscriptionResponse?> HandleAsync(
        GetCurrentSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required.");
        var billingAccount = await billingAccountRepository.FirstOrDefaultAsync(
            new BillingAccountByOwnerSpecification(
                tenantId,
                command.BillingAccountOwnerType,
                command.BillingAccountOwnerType == BillingAccountOwnerType.Stakeholder ? stakeholderId : null),
            cancellationToken);
        if (billingAccount is null)
        {
            return null;
        }

        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new CurrentSubscriptionByBillingAccountSpecification(tenantId, billingAccount.Id),
            cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        var plan = await planRepository.FirstOrDefaultAsync(
            new SubscriptionPlanByIdSpecification(subscription.PlanId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Plan '{subscription.PlanId}' was not found.");

        return new GetCurrentSubscriptionResponse(
            subscription.Id,
            billingAccount.Id,
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Price,
            plan.CurrencyId,
            plan.BillingInterval.ToString(),
            subscription.Status.ToString(),
            subscription.CurrentPeriodStartUtc,
            subscription.CurrentPeriodEndUtc,
            subscription.NextBillingAtUtc,
            subscription.AutoRenew,
            subscription.CancelAtPeriodEnd,
            subscription.CancellationRequestedAtUtc);
    }
}
