using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_NormalizingSubscriptionRevenue_WithUndefinedBillingInterval_Should
{
    [Fact]
    public void RejectTheInterval()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            SubscriptionRevenueMath.NormalizeMrr(300m, (BillingInterval)99));
    }
}
