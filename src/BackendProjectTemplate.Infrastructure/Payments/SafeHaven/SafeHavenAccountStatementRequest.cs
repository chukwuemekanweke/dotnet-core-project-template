namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementRequest(
    int Page,
    int Limit,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Type);
