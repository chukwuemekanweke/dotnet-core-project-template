using BackendProjectTemplate.Domain.Kyc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Kyc.Kycaid;

internal sealed class KycaidCallbackSignatureValidator(IOptions<KycaidOptions> options) : IKycCallbackSignatureValidator
{
    public bool IsValid(ReadOnlySpan<byte> rawPayload, string? signature)
    {
        if (rawPayload.IsEmpty || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(options.Value.ApiKey)) return false;
        var base64Payload = Convert.ToBase64String(rawPayload);
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(options.Value.ApiKey), Encoding.UTF8.GetBytes(base64Payload));
        var expected = Encoding.ASCII.GetBytes(Convert.ToHexString(hash).ToLowerInvariant());
        var received = Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant());
        return received.Length == expected.Length && CryptographicOperations.FixedTimeEquals(received, expected);
    }
}
