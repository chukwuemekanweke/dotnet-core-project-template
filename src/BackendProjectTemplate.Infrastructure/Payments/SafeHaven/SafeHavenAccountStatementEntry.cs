using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementEntry(
    bool IsReversal,
    [property: JsonPropertyName("_id")] string Id,
    string? CbaTransactionId,
    string Client,
    SafeHavenAccountStatementAccount Account,
    string PaymentReference,
    string Type,
    string Provider,
    string ProviderChannel,
    string PaymentServices,
    string Narration,
    decimal Amount,
    decimal RunningBalance,
    DateTimeOffset TransactionDate,
    DateTimeOffset ValueDate,
    [property: JsonPropertyName("__v")] int Version);
