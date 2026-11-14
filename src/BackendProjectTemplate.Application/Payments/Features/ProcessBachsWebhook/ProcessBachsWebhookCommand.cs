namespace BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;

public sealed record ProcessBachsWebhookCommand(
    BachsWebhook Webhook,
    string RawPayload,
    byte[] RawPayloadBytes,
    string? SignatureV2Header,
    string? SignatureHeader,
    string? TimestampHeader);

public sealed record BachsWebhook(
    string Id,
    string Type,
    DateTimeOffset CreatedAt,
    string OrganizationId,
    BachsWebhookData Data);

public sealed record BachsWebhookData(
    string? ChargeId,
    string? CheckoutId,
    string? Reference,
    string? Status,
    string? Amount,
    string? Currency);
