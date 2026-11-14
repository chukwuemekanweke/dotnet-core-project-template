using System.Text.Json.Serialization;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Bachs;

public sealed record BachsWebhookRequest(
    string Id,
    string Type,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("organization_id")] string OrganizationId,
    BachsWebhookDataRequest Data);

public sealed record BachsWebhookDataRequest(
    [property: JsonPropertyName("charge_id")] string? ChargeId,
    [property: JsonPropertyName("checkout_id")] string? CheckoutId,
    string? Reference,
    string? Status,
    string? Amount,
    string? Currency,
    [property: JsonPropertyName("payment_method_id")] string? PaymentMethodId,
    BachsWebhookCustomerRequest? Customer);

public sealed record BachsWebhookCustomerRequest(
    [property: JsonPropertyName("customer_id")] string? CustomerId,
    string? Email,
    string? Name);
