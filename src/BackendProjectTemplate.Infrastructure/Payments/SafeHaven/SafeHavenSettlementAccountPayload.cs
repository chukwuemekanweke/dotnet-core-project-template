namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed record SafeHavenSettlementAccountPayload(
    string BankCode,
    string AccountNumber);
