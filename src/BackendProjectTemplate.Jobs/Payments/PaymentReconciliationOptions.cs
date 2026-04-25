namespace BackendProjectTemplate.Jobs.Payments;

public sealed class PaymentReconciliationOptions
{
    public const string SectionName = "Jobs:Payments:Reconciliation";

    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 30;
    public int StaleThresholdMinutes { get; set; } = 5;
    public int RecheckDelayMinutes { get; set; } = 2;
}
