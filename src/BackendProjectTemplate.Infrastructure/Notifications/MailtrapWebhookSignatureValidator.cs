using BackendProjectTemplate.Domain.Notifications.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class MailtrapWebhookSignatureValidator(IOptions<EmailNotificationsOptions> options) : IMailtrapWebhookSignatureValidator
{
    private const string InvalidSignatureReason = "invalid_signature";
    private const string MissingSignatureReason = "missing_signature";
    private const string MissingSigningSecretReason = "missing_signing_secret";
    private const string MissingPayloadReason = "missing_payload";

    public Task<MailtrapWebhookSignatureValidationResult> ValidateAsync(
        MailtrapWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken)
    {
        var signingSecret = NormalizeOptional(options.Value.Mailtrap.WebhookSigningSecret);
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, MissingSigningSecretReason));
        }

        var signatureHeader = NormalizeOptional(request.SignatureHeader);
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, MissingSignatureReason));
        }

        if (string.IsNullOrWhiteSpace(request.RawPayload))
        {
            return Task.FromResult(new MailtrapWebhookSignatureValidationResult(false, MissingPayloadReason));
        }

        var expectedSignature = ComputeSignature(signingSecret, request.RawPayload);
        var isValid = VerifyWebhook(signatureHeader, expectedSignature);

        return Task.FromResult(new MailtrapWebhookSignatureValidationResult(
            isValid,
            isValid ? "signature_verified" : InvalidSignatureReason));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeSignature(string signingSecret, string rawPayload)
    {
        var key = Encoding.UTF8.GetBytes(signingSecret);
        var payload = Encoding.UTF8.GetBytes(rawPayload);
        var hash = HMACSHA256.HashData(key, payload);

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
