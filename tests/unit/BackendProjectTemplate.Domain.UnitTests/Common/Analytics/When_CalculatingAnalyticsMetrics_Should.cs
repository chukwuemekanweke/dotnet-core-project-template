using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Domain.UnitTests.Common.Analytics;

public sealed class When_CalculatingAnalyticsMetrics_Should
{
    [Fact]
    public void CalculateRoundedPercentageAndAverage()
    {
        AnalyticsMath.Percentage(1, 3).ShouldBe(33.33m);
        AnalyticsMath.Average(10m, 3).ShouldBe(3.33m);
    }

    [Fact]
    public void ReturnZeroForEmptyDenominators()
    {
        AnalyticsMath.Percentage(0, 0).ShouldBe(0m);
        AnalyticsMath.Average(0m, 0).ShouldBe(0m);
    }
}
