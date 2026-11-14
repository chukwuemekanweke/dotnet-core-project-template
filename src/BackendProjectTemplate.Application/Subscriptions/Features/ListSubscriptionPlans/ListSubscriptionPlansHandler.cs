using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;

public sealed class ListSubscriptionPlansHandler(
    IReadRepository<SubscriptionPlan> planRepository,
    IReadRepository<Currency> currencyRepository)
{
    public async Task<IReadOnlyList<ListSubscriptionPlansResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var plans = await planRepository.ListAsync(new ActiveSubscriptionPlansSpecification(), cancellationToken);
        var result = new List<ListSubscriptionPlansResponse>(plans.Count);
        foreach (var plan in plans)
        {
            var currency = await currencyRepository.GetByIdAsync(plan.CurrencyId, cancellationToken)
                ?? throw new InvalidOperationException($"Currency '{plan.CurrencyId}' was not found for plan '{plan.Code}'.");
            result.Add(new ListSubscriptionPlansResponse(
                plan.Id,
                plan.Code,
                plan.Name,
                plan.Price,
                plan.CurrencyId,
                currency.CurrencyCode,
                plan.BillingInterval.ToString()));
        }

        return result;
    }
}
