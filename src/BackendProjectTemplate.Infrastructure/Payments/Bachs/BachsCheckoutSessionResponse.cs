using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed record BachsCheckoutSessionResponse(
    [property: JsonPropertyName("checkout_id")] string? CheckoutId,
    [property: JsonPropertyName("checkout_url")] string? CheckoutUrl,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("payment_status")] string? PaymentStatus,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("reference")] string? Reference,
    [property: JsonPropertyName("expires_at")] DateTimeOffset? ExpiresAt,
    [property: JsonPropertyName("charge")] BachsCheckoutCharge? Charge);

internal sealed record BachsCheckoutCharge(
    [property: JsonPropertyName("payment_id")] string? PaymentId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("currency")] string? Currency);
