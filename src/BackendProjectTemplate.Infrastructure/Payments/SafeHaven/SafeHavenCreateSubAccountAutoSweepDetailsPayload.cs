namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed record SafeHavenCreateSubAccountAutoSweepDetailsPayload(
    string AccountNumber,
    string Schedule);
