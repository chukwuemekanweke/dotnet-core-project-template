namespace BackendProjectTemplate.Jobs.Authentication;

public sealed class IpAddressLocationEnrichmentOptions
{
    public const string SectionName = "Authentication:IpAddressLocationEnrichment";

    public int BatchSize { get; init; } = 50;
    public int PollIntervalSeconds { get; init; } = 300;
    public int LocationRefreshIntervalDays { get; init; } = 7;
}
