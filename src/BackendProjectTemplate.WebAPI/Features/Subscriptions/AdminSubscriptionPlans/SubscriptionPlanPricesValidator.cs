using BackendProjectTemplate.Domain.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

/// <summary>Validates a plan's per-interval prices: at least one, each interval priced at most once.</summary>
public sealed class SubscriptionPlanPricesValidator : AbstractValidator<IReadOnlyList<SubscriptionPlanPriceRequest>>
{
    public SubscriptionPlanPricesValidator()
    {
        RuleFor(prices => prices)
            .NotEmpty()
            .WithMessage("At least one billing interval price is required.");
        RuleFor(prices => prices)
            .Must(HaveDistinctIntervals)
            .When(prices => prices is { Count: > 0 })
            .WithMessage("Each billing interval may be priced only once.");
        RuleForEach(prices => prices).ChildRules(price =>
        {
            price.RuleFor(item => item.BillingInterval)
                .Must(StrictEnumParser.IsValid<BillingInterval>)
                .WithMessage("BillingInterval must be Monthly or Yearly.");
            price.RuleFor(item => item.Price)
                .GreaterThanOrEqualTo(0)
                .Must(value => decimal.Truncate(value) == value)
                .WithMessage("Price must be a whole number of minor currency units.");
        });
    }

    public static IReadOnlyDictionary<BillingInterval, decimal> ToPriceMap(IEnumerable<SubscriptionPlanPriceRequest> prices) =>
        prices.ToDictionary(price => StrictEnumParser.Parse<BillingInterval>(price.BillingInterval), price => price.Price);

    private static bool HaveDistinctIntervals(IReadOnlyList<SubscriptionPlanPriceRequest> prices) =>
        prices
            .Where(price => StrictEnumParser.IsValid<BillingInterval>(price.BillingInterval))
            .GroupBy(price => StrictEnumParser.Parse<BillingInterval>(price.BillingInterval))
            .All(group => group.Count() == 1);
}
