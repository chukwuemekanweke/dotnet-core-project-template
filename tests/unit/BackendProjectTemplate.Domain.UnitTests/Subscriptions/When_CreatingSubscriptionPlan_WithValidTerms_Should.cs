using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CreatingSubscriptionPlan_WithValidTerms_Should
{
    [Fact]
    public void NormalizeCodeAndUseCalendarMonth()
    {
        var plan = SubscriptionPlan.Create(
            " starter_monthly ",
            "Starter Monthly",
            500000m,
            Guid.CreateVersion7(),
            BillingInterval.Monthly,
            true);

        plan.Code.ShouldBe("STARTER_MONTHLY");
        plan.GetPrice(BillingInterval.Monthly).ShouldBe(500000m);
        plan.Offers(BillingInterval.Yearly).ShouldBeFalse();
        plan.CalculatePeriodEnd(BillingInterval.Monthly, new DateTimeOffset(2028, 1, 31, 10, 0, 0, TimeSpan.Zero))
            .ShouldBe(new DateTimeOffset(2028, 2, 29, 10, 0, 0, TimeSpan.Zero));
        Should.Throw<ArgumentException>(() => plan.GetPrice(BillingInterval.Yearly));
    }

    [Fact]
    public void OfferMonthlyAndAnnualPricesWithCardContentOnOnePlan()
    {
        var plan = SubscriptionPlan.Create(
            "standard",
            "Standard",
            Guid.CreateVersion7(),
            new Dictionary<BillingInterval, decimal>
            {
                [BillingInterval.Yearly] = 15000000m,
                [BillingInterval.Monthly] = 1500000m
            },
            true,
            "  More power for growing teams. ",
            ["All Basic features", " Up to 10 team members "]);

        plan.Code.ShouldBe("STANDARD");
        plan.Prices.Select(price => price.BillingInterval).ShouldBe([BillingInterval.Monthly, BillingInterval.Yearly]);
        plan.Prices.ShouldAllBe(price => price.PlanId == plan.Id);
        plan.GetPrice(BillingInterval.Monthly).ShouldBe(1500000m);
        plan.GetPrice(BillingInterval.Yearly).ShouldBe(15000000m);
        plan.Description.ShouldBe("More power for growing teams.");
        plan.Features.ShouldBe(["All Basic features", "Up to 10 team members"]);
    }

    [Fact]
    public void RejectInvalidCardContentOrMissingPrices()
    {
        var currencyId = Guid.CreateVersion7();
        var monthly = new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 1m };

        Should.Throw<ArgumentException>(() => SubscriptionPlan.Create("P", "P", currencyId, new Dictionary<BillingInterval, decimal>(), true));
        Should.Throw<ArgumentException>(() => SubscriptionPlan.Create("P", "P", currencyId, monthly, true, null, ["ok", " "]));
        Should.Throw<ArgumentException>(() => SubscriptionPlan.Create(
            "P", "P", currencyId, monthly, true, new string('d', SubscriptionPlan.MaxDescriptionLength + 1)));
        Should.Throw<ArgumentException>(() => SubscriptionPlan.Create(
            "P", "P", currencyId, monthly, true, null, Enumerable.Repeat("f", SubscriptionPlan.MaxFeatureCount + 1).ToList()));
    }
}
