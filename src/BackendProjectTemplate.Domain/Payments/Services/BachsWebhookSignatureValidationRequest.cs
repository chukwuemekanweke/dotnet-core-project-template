namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record BachsWebhookSignatureValidationRequest(
    byte[] RawBody,
    string? SignatureV2Header,
    string? SignatureHeader,
    string? TimestampHeader);
