namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed class PaymentAnalyticsOptions
{
    public const string SectionName = "Payments:Analytics";
    public int DefaultRangeDays { get; set; } = 30;
    public int MaximumRangeDays { get; set; } = 366;
    public TimeSpan SummaryTtl { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan TimelineTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StatusesTtl { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan ProvidersTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IntentsTtl { get; set; } = TimeSpan.FromMinutes(5);
}
