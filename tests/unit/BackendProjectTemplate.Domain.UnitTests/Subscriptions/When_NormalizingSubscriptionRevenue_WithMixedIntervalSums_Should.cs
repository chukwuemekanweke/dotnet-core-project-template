using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_NormalizingSubscriptionRevenue_WithMixedIntervalSums_Should
{
    [Fact]
    public void PreserveLinearityAndDeriveArrFromMrr()
    {
        var mixedMrr = SubscriptionRevenueMath.NormalizeMrr(100m + 300m, BillingInterval.Monthly) +
            SubscriptionRevenueMath.NormalizeMrr(1_200m + 2_400m, BillingInterval.Yearly);

        mixedMrr.ShouldBe(700m);
        SubscriptionRevenueMath.ToArr(mixedMrr).ShouldBe(8_400m);
    }
}
