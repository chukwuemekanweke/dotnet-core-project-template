namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_UpdatingSubscriptionPlanTerms_Should
{
    [Fact]
    public void ChangeOnlyMutableTermsAndLeaveHistoricalSnapshotsIntact()
    {
        var plan = SubscriptionAllocationTestData.CreatePlan(price: 500000m);
        var code = plan.Code;
        var subscription = SubscriptionAllocationTestData.Allocate(plan, SubscriptionAllocationTestData.Now, SubscriptionAllocationTestData.Now.AddDays(30));

        plan.UpdateTerms("  Premium Plus ", 650000m).ShouldBeTrue();
        plan.UpdateTerms("Premium Plus", 650000m).ShouldBeFalse();

        plan.Name.ShouldBe("Premium Plus");
        plan.Price.ShouldBe(650000m);
        plan.Code.ShouldBe(code);
        subscription.Periods.ShouldHaveSingleItem().Price.ShouldBe(500000m);
        Should.Throw<ArgumentOutOfRangeException>(() => plan.UpdateTerms("Premium", 10.5m));
        Should.Throw<ArgumentException>(() => plan.UpdateTerms(" ", 100m));
        plan.Price.ShouldBe(650000m);
    }
}
