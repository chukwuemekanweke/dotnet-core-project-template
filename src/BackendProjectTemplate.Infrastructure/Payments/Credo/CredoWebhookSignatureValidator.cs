using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal sealed class CredoWebhookSignatureValidator(IOptions<CredoOptions> options) : ICredoWebhookSignatureValidator
{
    private const string SignatureHeaderInvalidReason = "invalid_signature";
    private const string SignatureMissingReason = "missing_signature";
    private const string SecretKeyMissingReason = "missing_secret_key";
    private const string BusinessCodeMissingReason = "missing_business_code";

    public Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        CredoWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken)
    {
        var secretKey = NormalizeOptional(options.Value.SecretKey);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, SecretKeyMissingReason));
        }

        var signatureHeader = NormalizeOptional(request.SignatureHeader);
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, SignatureMissingReason));
        }

        var businessCode = NormalizeOptional(request.BusinessCode);
        if (string.IsNullOrWhiteSpace(businessCode))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, BusinessCodeMissingReason));
        }

        var expectedSignature = ComputeSignature(secretKey, businessCode);
        var validationStatus = VerifyWebhook(signatureHeader, expectedSignature)
            ? SignatureValidationStatus.Valid
            : SignatureValidationStatus.Invalid;
        var statusChangeReason = validationStatus == SignatureValidationStatus.Valid
            ? "signature_verified"
            : SignatureHeaderInvalidReason;

        return Task.FromResult(new PaymentProviderWebhookValidationResult(validationStatus, statusChangeReason));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeSignature(string secretKey, string businessCode)
    {
        using var sha512 = SHA512.Create();
        var payload = Encoding.UTF8.GetBytes(secretKey + businessCode);
        var hash = sha512.ComputeHash(payload);

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static bool VerifyWebhook(string signature, string expectedSignature) =>
        expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
}
