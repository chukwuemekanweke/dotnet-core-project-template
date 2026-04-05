namespace BackendProjectTemplate.Jobs;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 10;
}
