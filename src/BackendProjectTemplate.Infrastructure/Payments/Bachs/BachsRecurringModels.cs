using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed record BachsCreateCustomerRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata);

internal sealed record BachsCustomerResponse(
    [property: JsonPropertyName("customer_id")] string? CustomerId,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("name")] string? Name);

internal sealed record BachsCreateChargeRequest(
    [property: JsonPropertyName("customer")] string Customer,
    [property: JsonPropertyName("payment_method")] string PaymentMethod,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata);

internal sealed record BachsChargeResponse(
    [property: JsonPropertyName("payment_id")] string? PaymentId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("reference")] string? Reference,
    [property: JsonPropertyName("failure_code")] string? FailureCode,
    [property: JsonPropertyName("failure_reason")] string? FailureReason);
