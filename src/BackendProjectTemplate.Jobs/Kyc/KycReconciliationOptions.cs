namespace BackendProjectTemplate.Jobs.Kyc;

public sealed class KycReconciliationOptions
{
    public const string SectionName = "Jobs:Kyc:Reconciliation";

    public int BatchSize { get; set; } = 50;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan RecheckDelay { get; set; } = TimeSpan.FromMinutes(30);
}
