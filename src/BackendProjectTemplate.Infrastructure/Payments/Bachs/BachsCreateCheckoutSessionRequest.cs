using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed record BachsCreateCheckoutSessionRequest(
    [property: JsonPropertyName("pricing")] BachsCheckoutPricing Pricing,
    [property: JsonPropertyName("customer")] BachsCheckoutCustomer Customer,
    [property: JsonPropertyName("success_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SuccessUrl,
    [property: JsonPropertyName("cancel_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CancelUrl,
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata,
    [property: JsonPropertyName("expires_in_minutes")] int ExpiresInMinutes);

internal sealed record BachsCheckoutPricing(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("amount")] string Amount);

internal sealed record BachsCheckoutCustomer(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);
