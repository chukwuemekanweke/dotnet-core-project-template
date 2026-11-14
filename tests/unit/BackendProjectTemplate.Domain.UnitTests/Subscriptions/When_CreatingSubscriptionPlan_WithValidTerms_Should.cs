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
        plan.Price.ShouldBe(500000m);
        plan.CalculatePeriodEnd(new DateTimeOffset(2028, 1, 31, 10, 0, 0, TimeSpan.Zero))
            .ShouldBe(new DateTimeOffset(2028, 2, 29, 10, 0, 0, TimeSpan.Zero));
    }
}
