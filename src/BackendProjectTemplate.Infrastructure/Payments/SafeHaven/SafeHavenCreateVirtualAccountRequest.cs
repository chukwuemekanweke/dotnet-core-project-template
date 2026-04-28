namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenCreateVirtualAccountRequest(
    string ExternalReference,
    string? AccountName = null,
    decimal? Amount = null);
