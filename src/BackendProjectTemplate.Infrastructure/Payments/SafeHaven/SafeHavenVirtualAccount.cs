using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenVirtualAccount(
    [property: JsonPropertyName("_id")] 
    string Id,
    string Client,
    string BankCode,
    string AccountNumber,
    string AccountName,
    string CurrencyCode,
    string Bvn,
    int ValidFor,
    string AmountControl,
    decimal Amount,
    DateTimeOffset ExpiryDate,
    string CallbackUrl,
    SafeHavenSettlementAccount SettlementAccount,
    string Status,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
