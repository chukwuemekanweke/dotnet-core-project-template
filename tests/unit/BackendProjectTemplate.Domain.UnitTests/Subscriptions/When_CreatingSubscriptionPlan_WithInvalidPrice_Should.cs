using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CreatingSubscriptionPlan_WithInvalidPrice_Should
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1.5)]
    public void RejectPrice(decimal price)
    {
        var action = () => SubscriptionPlan.Create(
            "PLAN",
            "Plan",
            price,
            Guid.CreateVersion7(),
            BillingInterval.Monthly,
            true);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }
}
