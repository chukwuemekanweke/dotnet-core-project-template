namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed record SafeHavenInitiateVerificationPayload(
    string Type,
    int Number,
    int DebitAccountNumber,
    bool Async);
