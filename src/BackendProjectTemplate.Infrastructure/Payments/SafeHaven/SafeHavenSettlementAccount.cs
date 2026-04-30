namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenSettlementAccount(
    string BankCode,
    string AccountNumber);
