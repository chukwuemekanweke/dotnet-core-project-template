namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed class SubscriptionAnalyticsOptions
{
    public const string SectionName = "Subscriptions:Analytics";
    public int DefaultRangeDays { get; set; } = 30;
    public int MaximumRangeDays { get; set; } = 366;
    public TimeSpan SummaryTtl { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan TimelineTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BreakdownTtl { get; set; } = TimeSpan.FromMinutes(3);
}
