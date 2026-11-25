using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_NormalizingSubscriptionRevenue_WithSupportedBillingIntervals_Should
{
    [Fact]
    public void PreserveMonthlyPriceAndAnnualDecimalPrecision()
    {
        SubscriptionRevenueMath.NormalizeMrr(1_200m, BillingInterval.Monthly).ShouldBe(1_200m);
        SubscriptionRevenueMath.NormalizeMrr(1_000m, BillingInterval.Yearly)
            .ShouldBe(1_000m / 12m);
    }
}
