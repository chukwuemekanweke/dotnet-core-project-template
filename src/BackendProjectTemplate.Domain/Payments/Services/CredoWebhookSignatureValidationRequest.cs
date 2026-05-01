namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record CredoWebhookSignatureValidationRequest(
    string? SignatureHeader,
    string? BusinessCode);
