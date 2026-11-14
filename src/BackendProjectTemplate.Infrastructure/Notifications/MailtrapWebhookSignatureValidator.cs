using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Notifications.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class MailtrapWebhookSignatureValidator(IOptions<EmailNotificationsOptions> options) : IMailtrapWebhookSignatureValidator
{
    public Task<MailtrapWebhookSignatureValidationResult> ValidateAsync(
        MailtrapWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken)
    {
        var signingSecret = NormalizeOptional(options.Value.Mailtrap.WebhookSigningSecret);
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Notifications.MissingSigningSecret));
        }

        var signatureHeader = NormalizeOptional(request.SignatureHeader);
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Shared.MissingSignature));
        }

        if (request.RawPayload.Length == 0)
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Notifications.MissingPayload));
        }

        var expectedSignature = ComputeSignature(signingSecret, request.RawPayload);
        var isValid = VerifyWebhook(signatureHeader, expectedSignature);

        return Task.FromResult(new MailtrapWebhookSignatureValidationResult(
            isValid,
            isValid ? KnownWebhookStatusChangeReasons.Shared.SignatureVerified : KnownWebhookStatusChangeReasons.Shared.InvalidSignature));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeSignature(string signingSecret, byte[] rawPayload)
    {
        var key = Encoding.UTF8.GetBytes(signingSecret);
        var hash = HMACSHA256.HashData(key, rawPayload);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool VerifyWebhook(string signature, string expectedSignature)
    {
        var signatureBytes = Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant());
        var expectedBytes = Encoding.ASCII.GetBytes(expectedSignature);

        return signatureBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(signatureBytes, expectedBytes);
    }
}
