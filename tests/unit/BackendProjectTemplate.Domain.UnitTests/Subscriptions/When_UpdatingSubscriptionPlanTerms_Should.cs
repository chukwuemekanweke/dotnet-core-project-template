using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_UpdatingSubscriptionPlanTerms_Should
{
    [Fact]
    public void ChangeOnlyMutableTermsAndLeaveHistoricalSnapshotsIntact()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan(price: 500000m);
        var code = plan.Code;
        var subscription = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, SubscriptionAllocationTestData.Now.AddDays(30));

        plan.UpdateTerms("  Premium Plus ", Monthly(650000m), " For growing teams ", [" Priority support "]).ShouldBeTrue();
        plan.UpdateTerms("Premium Plus", Monthly(650000m), "For growing teams", ["Priority support"]).ShouldBeFalse();

        plan.Name.ShouldBe("Premium Plus");
        plan.Description.ShouldBe("For growing teams");
        plan.Features.ShouldBe(["Priority support"]);
        plan.GetPrice(BillingInterval.Monthly).ShouldBe(650000m);
        plan.Code.ShouldBe(code);
        subscription.Periods.ShouldHaveSingleItem().Price.ShouldBe(500000m);
        Should.Throw<ArgumentOutOfRangeException>(() => plan.UpdateTerms("Premium", Monthly(10.5m), null, []));
        Should.Throw<ArgumentException>(() => plan.UpdateTerms(" ", Monthly(100m), null, []));
        plan.GetPrice(BillingInterval.Monthly).ShouldBe(650000m);
    }

    [Fact]
    public void AddANewIntervalButRefuseToWithdrawAnOfferedOne()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan(price: 500000m);

        plan.UpdateTerms(
            plan.Name,
            new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 500000m, [BillingInterval.Yearly] = 5000000m },
            null,
            []).ShouldBeTrue();

        plan.Offers(BillingInterval.Yearly).ShouldBeTrue();
        plan.GetPrice(BillingInterval.Yearly).ShouldBe(5000000m);
        Should.Throw<ArgumentException>(() => plan.UpdateTerms(
            plan.Name,
            new Dictionary<BillingInterval, decimal> { [BillingInterval.Yearly] = 5000000m },
            null,
            []));
        plan.Offers(BillingInterval.Monthly).ShouldBeTrue();
    }

    [Fact]
    public void ClearDescriptionAndFeaturesWhenOmitted()
    {
        var plan = SubscriptionPlan.Create("BASIC", "Basic", Guid.CreateVersion7(), Monthly(500000m), true, "Starter", ["Email support"]);

        plan.UpdateTerms("Basic", Monthly(500000m), null, null).ShouldBeTrue();

        plan.Description.ShouldBeNull();
        plan.Features.ShouldBeEmpty();
    }

    private static Dictionary<BillingInterval, decimal> Monthly(decimal price) =>
        new() { [BillingInterval.Monthly] = price };
}
