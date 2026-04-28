namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAutoSweepDetails(
    string Schedule = "Instant",
    int AccountNumber);
