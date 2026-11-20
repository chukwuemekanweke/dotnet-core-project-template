namespace BackendProjectTemplate.Application.Analytics;

public sealed class AdministrativeAnalyticsOptions
{
    public const string SectionName = "Analytics:AdministrativeUsers";
    public int DefaultRangeDays { get; set; } = 30;
    public int MaximumRangeDays { get; set; } = 366;
    public TimeSpan RegistrationTtl { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan ActiveUsersTtl { get; set; } = TimeSpan.FromMinutes(1);
}
