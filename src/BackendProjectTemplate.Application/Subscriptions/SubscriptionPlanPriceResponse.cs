using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions;

public sealed record SubscriptionPlanPriceResponse(
    string BillingInterval,
    decimal Price)
{
    public static IReadOnlyList<SubscriptionPlanPriceResponse> From(IEnumerable<SubscriptionPlanPrice> prices) =>
        From(prices.Select(price => (price.BillingInterval, price.Price)));

    public static IReadOnlyList<SubscriptionPlanPriceResponse> From(IEnumerable<SubscriptionPlanPriceReadModel> prices) =>
        From(prices.Select(price => (price.BillingInterval, price.Price)));

    private static IReadOnlyList<SubscriptionPlanPriceResponse> From(IEnumerable<(BillingInterval BillingInterval, decimal Price)> prices) =>
        prices
            .OrderBy(price => price.BillingInterval)
            .Select(price => new SubscriptionPlanPriceResponse(price.BillingInterval.ToString(), price.Price))
            .ToList();
}
