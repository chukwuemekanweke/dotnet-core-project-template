namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAutoSweepDetails(
    int AccountNumber,
    string Schedule = "Instant");
