using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Payments.Services;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed class BachsWebhookSignatureValidator(
    IOptions<BachsOptions> options,
    TimeProvider timeProvider) : IBachsWebhookSignatureValidator
{
    public Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        BachsWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken)
    {
        var secret = NormalizeOptional(options.Value.WebhookSecret);
        if (secret is null)
        {
            return Invalid(KnownWebhookStatusChangeReasons.Payments.MissingSecretKey);
        }

        var parsed = ParseSignatures(request);
        if (parsed is null || parsed.Signatures.Count == 0)
        {
            return Invalid(KnownWebhookStatusChangeReasons.Shared.MissingSignature);
        }

        var tolerance = options.Value.WebhookTimestampToleranceSeconds;
        if (tolerance <= 0 ||
            Math.Abs(timeProvider.GetUtcNow().ToUnixTimeSeconds() - parsed.Timestamp) > tolerance)
        {
            return Invalid(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
        }

        var prefix = Encoding.UTF8.GetBytes($"{parsed.Timestamp}.");
        var body = request.RawBody;
        var signedPayload = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, signedPayload, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, signedPayload, prefix.Length, body.Length);
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signedPayload);
        var valid = parsed.Signatures.Any(signature => IsMatchingHexSignature(expected, signature));

        return Task.FromResult(new PaymentProviderWebhookValidationResult(
            valid ? SignatureValidationStatus.Valid : SignatureValidationStatus.Invalid,
            valid
                ? KnownWebhookStatusChangeReasons.Shared.SignatureVerified
                : KnownWebhookStatusChangeReasons.Shared.InvalidSignature));
    }

    private static ParsedSignatures? ParseSignatures(BachsWebhookSignatureValidationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SignatureV2Header))
        {
            long? timestamp = null;
            var signatures = new List<string>();
            foreach (var part in request.SignatureV2Header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (pair.Length != 2)
                {
                    continue;
                }

                if (pair[0] == "t" && long.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
                else if (pair[0] == "v1" && !string.IsNullOrWhiteSpace(pair[1]))
                {
                    signatures.Add(pair[1]);
                }
            }

            return timestamp.HasValue ? new ParsedSignatures(timestamp.Value, signatures) : null;
        }

        return long.TryParse(request.TimestampHeader, NumberStyles.None, CultureInfo.InvariantCulture, out var legacyTimestamp) &&
               !string.IsNullOrWhiteSpace(request.SignatureHeader)
            ? new ParsedSignatures(legacyTimestamp, [request.SignatureHeader.Trim()])
            : null;
    }

    private static bool IsMatchingHexSignature(byte[] expected, string signature)
    {
        try
        {
            var actual = Convert.FromHexString(signature);
            return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static Task<PaymentProviderWebhookValidationResult> Invalid(string reason) =>
        Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, reason));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ParsedSignatures(long Timestamp, IReadOnlyList<string> Signatures);
}
