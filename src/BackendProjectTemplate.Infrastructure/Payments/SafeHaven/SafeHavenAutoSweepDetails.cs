namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAutoSweepDetails(
    string? SettlementAccountId,
    decimal? MinimumBalance);
