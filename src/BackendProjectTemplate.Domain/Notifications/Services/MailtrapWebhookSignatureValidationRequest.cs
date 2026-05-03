namespace BackendProjectTemplate.Domain.Notifications.Services;

public sealed record MailtrapWebhookSignatureValidationRequest(string? SignatureHeader, string RawPayload);
