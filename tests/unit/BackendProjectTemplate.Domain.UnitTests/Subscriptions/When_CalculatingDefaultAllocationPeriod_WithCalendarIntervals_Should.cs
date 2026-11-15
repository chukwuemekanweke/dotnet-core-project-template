using BackendProjectTemplate.Domain.Subscriptions;

namespace BackendProjectTemplate.Domain.UnitTests.Subscriptions;

public sealed class When_CalculatingDefaultAllocationPeriod_WithCalendarIntervals_Should
{
    [Fact]
    public void UseCalendarMonthAndYearArithmetic()
    {
        var monthly = SubscriptionAllocationTestData.CreatePlan(BillingInterval.Monthly);
        var yearly = SubscriptionAllocationTestData.CreatePlan(BillingInterval.Yearly);
        var endOfJanuary = DateTimeOffset.Parse("2027-01-31T08:00:00Z");
        var leapDay = DateTimeOffset.Parse("2028-02-29T08:00:00Z");

        monthly.CalculatePeriodEnd(endOfJanuary).ShouldBe(DateTimeOffset.Parse("2027-02-28T08:00:00Z"));
        yearly.CalculatePeriodEnd(leapDay).ShouldBe(DateTimeOffset.Parse("2029-02-28T08:00:00Z"));
        BillingIntervals.CalculatePeriodEnd(BillingInterval.Monthly, endOfJanuary).Offset.ShouldBe(TimeSpan.Zero);
    }
}
