namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementRequest(
    int Page = 0,
    int Limit = 100,
    string? FromDate = null,
    string? ToDate = null,
    string? Type = "debit");
