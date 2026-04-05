namespace BackendProjectTemplate.Jobs.OutboxProcessing;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "Jobs:OutboxProcessing";

    public int BatchSize { get; set; } = 50;

    public int PollIntervalSeconds { get; set; } = 5;
}
