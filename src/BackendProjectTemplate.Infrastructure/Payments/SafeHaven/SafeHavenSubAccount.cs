using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenSubAccount(
    [property: JsonPropertyName("_id")] string Id,
    string Client,
    string AccountProduct,
    string AccountNumber,
    string AccountName,
    string AccountType,
    string CurrencyCode,
    string Bvn,
    string IdentityId,
    decimal AccountBalance,
    decimal BookBalance,
    string CallbackUrl,
    bool IsSubAccount,
    SafeHavenSubAccountDetails SubAccountDetails,
    string ExternalReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Nin,
    [property: JsonPropertyName("__v")] int Version,
    string CbaAccountId);
