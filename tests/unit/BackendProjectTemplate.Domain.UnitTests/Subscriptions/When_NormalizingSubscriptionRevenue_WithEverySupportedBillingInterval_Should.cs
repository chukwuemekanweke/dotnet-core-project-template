using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_NormalizingSubscriptionRevenue_WithEverySupportedBillingInterval_Should
{
    [Fact]
    public void DivideByTheMonthsItCovers()
    {
        foreach (var interval in Enum.GetValues<BillingInterval>())
        {
            var months = SubscriptionRevenueMath.MonthsPerInterval(interval);
            SubscriptionRevenueMath.NormalizeMrr(months * 250m, interval).ShouldBe(250m);
        }

        SubscriptionRevenueMath.MonthsPerInterval(BillingInterval.Monthly).ShouldBe(1);
        SubscriptionRevenueMath.MonthsPerInterval(BillingInterval.Yearly).ShouldBe(12);
    }
}
