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

    [Fact]
    public void DivideEachIntervalByTheMonthsItCovers()
    {
        foreach (var interval in Enum.GetValues<BillingInterval>())
        {
            var months = SubscriptionRevenueMath.MonthsPerInterval(interval);
            SubscriptionRevenueMath.NormalizeMrr(months * 250m, interval).ShouldBe(250m);
        }

        SubscriptionRevenueMath.MonthsPerInterval(BillingInterval.Monthly).ShouldBe(1);
        SubscriptionRevenueMath.MonthsPerInterval(BillingInterval.Yearly).ShouldBe(12);
    }

    [Fact]
    public void NormalizeMixedIntervalSumsLinearlyAndDeriveArrFromMrr()
    {
        var mixedMrr = SubscriptionRevenueMath.NormalizeMrr(100m + 300m, BillingInterval.Monthly) +
            SubscriptionRevenueMath.NormalizeMrr(1_200m + 2_400m, BillingInterval.Yearly);

        mixedMrr.ShouldBe(700m);
        SubscriptionRevenueMath.ToArr(mixedMrr).ShouldBe(8_400m);
    }

    [Fact]
    public void RejectUndefinedIntervalsInsteadOfGuessingADivisor()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            SubscriptionRevenueMath.NormalizeMrr(300m, (BillingInterval)99));
    }
}
