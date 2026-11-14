using System.Text;

namespace BackendProjectTemplate.Domain.Notifications.Services;

public sealed record MailtrapWebhookSignatureValidationRequest(string? SignatureHeader, byte[] RawPayload)
{
    public MailtrapWebhookSignatureValidationRequest(string? signatureHeader, string rawPayload)
        : this(signatureHeader, Encoding.UTF8.GetBytes(rawPayload))
    {
    }
}
